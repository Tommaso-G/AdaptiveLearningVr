using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DifficultyChapterFilter))]
public class DiffiicultyChapterFilterEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        DifficultyChapterFilter filter = (DifficultyChapterFilter)target;

        if (GUILayout.Button("Carica capitoli"))
        {
            LoadChapters(filter);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void LoadChapters(DifficultyChapterFilter filter)
    {
        List<string> chaptersList = new List<string>();

        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process != null)
                foreach (var chapter in process.Data.Chapters)
                    chaptersList.Add(chapter.Data.Name);
        }
        else
        {
            var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
            chaptersList = data.chapters;
        }

        if (chaptersList == null || chaptersList.Count == 0)
        {
            Debug.LogWarning("[DifficutlyChapterFilterEditor] Nessun capitolo trovato.");
            return;
        }

        // Preserva i valori già impostati
        var existing = new Dictionary<string, int>();
        foreach (var s in filter.chapterSettings)
            existing[s.chapterName] = s.difficultyLevel;

        filter.chapterSettings.Clear();

        foreach (string name in chaptersList)
        {
            filter.chapterSettings.Add(new DifficultyChapterFilter.DifficultyChapterSetting
            {
                chapterName = name,
                difficultyLevel = existing.ContainsKey(name) ? existing[name] : 0
            });
        }

        EditorUtility.SetDirty(filter);
        Debug.Log($"[DifficutlyChapterFilterEditor] Caricati {filter.chapterSettings.Count} capitoli.");
    }
}
