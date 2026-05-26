using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(FeedbackIconsManager))]
public class FeedbackIconsManagerEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        FeedbackIconsManager manager = (FeedbackIconsManager)target;

        // Campi base
        manager.learningProfile = (LearningProfile)EditorGUILayout.ObjectField(
            "Learning Profile", manager.learningProfile, typeof(LearningProfile), true);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Percorso JSON processo", EditorStyles.boldLabel);
        defaultPath = EditorGUILayout.TextField(defaultPath);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Carica step", GUILayout.Height(28)))
        {
            LoadSteps(manager);
        }

        EditorGUILayout.Space(12);

        // Tabella raggruppata per capitolo
        foreach (var chapter in manager.chapterStepMappings)
        {
            EditorGUILayout.LabelField(chapter.chapterName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Header colonne
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Step", EditorStyles.miniBoldLabel, GUILayout.Width(220));
            EditorGUILayout.LabelField("Icon Controller", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var mapping in chapter.steps)
            {
                EditorGUILayout.BeginHorizontal();

                // stepName: label read-only
                EditorGUILayout.LabelField(mapping.stepName, GUILayout.Width(220));

                // iconController: campo oggetto editabile
                EditorGUI.BeginChangeCheck();
                FeedbackIconController newController = (FeedbackIconController)EditorGUILayout.ObjectField(
                    mapping.iconController, typeof(FeedbackIconController), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Modifica FeedbackIconController");
                    mapping.iconController = newController;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }
    }

    private void LoadSteps(FeedbackIconsManager manager)
    {
        List<string> rawChapters = new List<string>();
        List<string> rawSteps    = new List<string>();

        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process == null)
            {
                Debug.LogWarning("[FeedbackIconsManagerEditor] Nessun processo in esecuzione.");
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
            Debug.LogWarning("[FeedbackIconsManagerEditor] Nessuno step trovato.");
            return;
        }

        // Preserva i controller già assegnati — chiave: "chapterName::stepName"
        var existing = new Dictionary<string, FeedbackIconController>();
        foreach (var ch in manager.chapterStepMappings)
            foreach (var m in ch.steps)
                existing[$"{ch.chapterName}::{m.stepName}"] = m.iconController;

        manager.chapterStepMappings.Clear();

        FeedbackIconsManager.ChapterStepIconMapping currentChapter = null;

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

                currentChapter = new FeedbackIconsManager.ChapterStepIconMapping { chapterName = matchedName };
                manager.chapterStepMappings.Add(currentChapter);
                continue;
            }

            if (currentChapter == null)
            {
                currentChapter = new FeedbackIconsManager.ChapterStepIconMapping { chapterName = "Unknown" };
                manager.chapterStepMappings.Add(currentChapter);
            }

            string key = $"{currentChapter.chapterName}::{trimmed}";
            existing.TryGetValue(key, out FeedbackIconController savedController);

            currentChapter.steps.Add(new FeedbackIconsManager.StepIconMapping
            {
                stepName       = trimmed,
                iconController = savedController
            });
        }

        EditorUtility.SetDirty(manager);
        Debug.Log($"[FeedbackIconsManagerEditor] Caricati {manager.chapterStepMappings.Count} capitoli.");
    }
}
