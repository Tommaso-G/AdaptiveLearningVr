using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(StepNameAliasMap))]
public class StepNameAliasMapEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        StepNameAliasMap aliasMap = (StepNameAliasMap)target;

        EditorGUILayout.LabelField("Percorso JSON processo", EditorStyles.boldLabel);
        defaultPath = EditorGUILayout.TextField(defaultPath);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Carica step", GUILayout.Height(28)))
        {
            LoadSteps(aliasMap);
        }

        EditorGUILayout.Space(12);

        // ── Disegna la tabella manualmente ──────────────────────────────
        foreach (var chapter in aliasMap.chapterStepAliases)
        {
            // Header capitolo
            EditorGUILayout.LabelField(chapter.chapterName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Header colonne
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Step", EditorStyles.miniBoldLabel, GUILayout.Width(220));
            EditorGUILayout.LabelField("Display Name", EditorStyles.miniBoldLabel, GUILayout.Width(220));
            EditorGUILayout.LabelField("Peso", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            foreach (var alias in chapter.steps)
            {
                EditorGUILayout.BeginHorizontal();

                // stepName: label read-only
                EditorGUILayout.LabelField(alias.stepName, GUILayout.Width(220));

                // displayName: campo editabile
                EditorGUI.BeginChangeCheck();
                string newDisplay = EditorGUILayout.TextField(alias.displayName, GUILayout.Width(220));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(aliasMap, "Modifica Display Name");
                    alias.displayName = newDisplay;
                    EditorUtility.SetDirty(aliasMap);
                }

                // weight: campo editabile
                EditorGUI.BeginChangeCheck();
                int newWeight = EditorGUILayout.IntField(alias.weight, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(aliasMap, "Modifica Peso");
                    alias.weight = Mathf.Max(0, newWeight); // evita pesi negativi
                    EditorUtility.SetDirty(aliasMap);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }
    }

    private void LoadSteps(StepNameAliasMap aliasMap)
    {
        List<string> rawChapters = new List<string>();
        List<string> rawSteps    = new List<string>();

        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process == null)
            {
                Debug.LogWarning("[StepNameAliasMapEditor] Nessun processo in esecuzione.");
                return;
            }

            foreach (var chapter in process.Data.Chapters)
            {
                rawChapters.Add(chapter.Data.Name);
                rawSteps.Add($"--- {chapter.Data.Name.ToUpper()} ---");
                foreach (var step in chapter.Data.Steps)
                    rawSteps.Add(step.Data.Name);
            }
        }
        else
        {
            var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
            rawChapters = data.chapters;
            rawSteps    = data.steps;
        }

        if (rawSteps == null || rawSteps.Count == 0)
        {
            Debug.LogWarning("[StepNameAliasMapEditor] Nessuno step trovato.");
            return;
        }

        // Preserva i displayName già inseriti — chiave: "chapterName::stepName"
        var existing = new Dictionary<string, (string displayName, int weight)>();
        foreach (var ch in aliasMap.chapterStepAliases)
            foreach (var s in ch.steps)
                existing[$"{ch.chapterName}::{s.stepName}"] = (s.displayName, s.weight);

        aliasMap.chapterStepAliases.Clear();

        StepNameAliasMap.ChapterStepAlias currentChapter = null;

        foreach (string entry in rawSteps)
        {
            string trimmed = entry.Trim();

            // Separatore capitolo: "--- NOME ---"
            if (trimmed.StartsWith("---") && trimmed.EndsWith("---"))
            {
                string chapterName = trimmed.Replace("---", "").Trim();

                string matchedName = rawChapters.Find(c =>
                    string.Equals(c.ToUpper(), chapterName, System.StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(matchedName))
                    matchedName = chapterName;

                currentChapter = new StepNameAliasMap.ChapterStepAlias { chapterName = matchedName };
                aliasMap.chapterStepAliases.Add(currentChapter);
                continue;
            }

            if (currentChapter == null)
            {
                currentChapter = new StepNameAliasMap.ChapterStepAlias { chapterName = "Unknown" };
                aliasMap.chapterStepAliases.Add(currentChapter);
            }

            string key = $"{currentChapter.chapterName}::{trimmed}";
            bool found = existing.TryGetValue(key, out var savedData);

            currentChapter.steps.Add(new StepNameAliasMap.StepAlias
            {
                stepName = trimmed,
                displayName = found ? savedData.displayName : string.Empty,
                weight = found ? savedData.weight : 1
            });
        }

        EditorUtility.SetDirty(aliasMap);
        Debug.Log($"[StepNameAliasMapEditor] Caricati {aliasMap.chapterStepAliases.Count} capitoli.");
    }
}