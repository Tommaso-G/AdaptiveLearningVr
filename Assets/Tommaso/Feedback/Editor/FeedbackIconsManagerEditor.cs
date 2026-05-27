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

        manager.learningProfile = (LearningProfile)EditorGUILayout.ObjectField(
            "Learning Profile", manager.learningProfile, typeof(LearningProfile), true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Percorso JSON processo", EditorStyles.boldLabel);
        defaultPath = EditorGUILayout.TextField(defaultPath);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Carica step", GUILayout.Height(28)))
            LoadSteps(manager);

        EditorGUILayout.Space(12);

        // Tabella raggruppata per capitolo
        foreach (var chapter in manager.chapterStepMappings)
        {
            EditorGUILayout.LabelField(chapter.chapterName, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var mapping in chapter.steps)
            {
                EditorGUILayout.LabelField(mapping.stepName, EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                // Lista controller
                for (int i = 0; i < mapping.iconControllers.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginChangeCheck();
                    FeedbackIconController newController = (FeedbackIconController)EditorGUILayout.ObjectField(
                        mapping.iconControllers[i], typeof(FeedbackIconController), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Modifica FeedbackIconController");
                        mapping.iconControllers[i] = newController;
                        EditorUtility.SetDirty(manager);
                    }

                    // Bottone rimuovi
                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        Undo.RecordObject(manager, "Rimuovi FeedbackIconController");
                        mapping.iconControllers.RemoveAt(i);
                        EditorUtility.SetDirty(manager);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // Bottone aggiungi slot
                if (GUILayout.Button("+ Aggiungi controller"))
                {
                    Undo.RecordObject(manager, "Aggiungi FeedbackIconController");
                    mapping.iconControllers.Add(null);
                    EditorUtility.SetDirty(manager);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
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

        // Preserva i controller già assegnati — chiave: "chapterName::stepName" → List<FeedbackIconController>
        var existing = new Dictionary<string, List<FeedbackIconController>>();
        foreach (var ch in manager.chapterStepMappings)
            foreach (var m in ch.steps)
                existing[$"{ch.chapterName}::{m.stepName}"] = new List<FeedbackIconController>(m.iconControllers);

        manager.chapterStepMappings.Clear();

        FeedbackIconsManager.ChapterStepIconMapping currentChapter = null;

        foreach (string entry in rawSteps)
        {
            string trimmed = entry.Trim();

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
            existing.TryGetValue(key, out List<FeedbackIconController> savedControllers);

            currentChapter.steps.Add(new FeedbackIconsManager.StepIconMapping
            {
                stepName       = trimmed,
                iconControllers = savedControllers ?? new List<FeedbackIconController>()
            });
        }

        EditorUtility.SetDirty(manager);
        Debug.Log($"[FeedbackIconsManagerEditor] Caricati {manager.chapterStepMappings.Count} capitoli.");
    }
}