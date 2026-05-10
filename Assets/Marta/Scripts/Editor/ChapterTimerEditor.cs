using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChapterTimer))]
public class ChapterTimerEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        ChapterTimer filter = (ChapterTimer)target;

        if (GUILayout.Button("Carica capitoli"))
        {
            LoadChapters(filter);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void LoadChapters(ChapterTimer filter)
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
            Debug.LogWarning("[ChapterTimerEditor] Nessun capitolo trovato.");
            return;
        }

        // Preserva i valori già impostati
        var existing = new Dictionary<string, float>();
        foreach (var s in filter.timerSettings)
            existing[s.chapterWithTimer] = s.max_time;

        filter.timerSettings.Clear();

        foreach (string name in chaptersList)
        {
            filter.timerSettings.Add(new ChapterTimer.ChapterTimerSettings
            {
                chapterWithTimer = name,
                max_time = existing.ContainsKey(name) ? existing[name] : 0
            });
        }

        EditorUtility.SetDirty(filter);
        Debug.Log($"[ChapterTimerEditor] Caricati {filter.timerSettings.Count} capitoli.");
    }
}
