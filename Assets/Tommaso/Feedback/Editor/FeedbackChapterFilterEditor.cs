using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(FeedbackChapterFilter))]
public class FeedbackChapterFilterEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        FeedbackChapterFilter filter = (FeedbackChapterFilter)target;

        if (GUILayout.Button("Carica capitoli"))
        {
            LoadChapters(filter);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void LoadChapters(FeedbackChapterFilter filter)
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
            Debug.LogWarning("[FeedbackChapterFilterEditor] Nessun capitolo trovato.");
            return;
        }

        // Preserva i valori già impostati
        var existing = new Dictionary<string, int>();
        foreach (var s in filter.chapterSettings)
            existing[s.chapterName] = s.feedbackLevel;

        filter.chapterSettings.Clear();

        foreach (string name in chaptersList)
        {
            filter.chapterSettings.Add(new FeedbackChapterFilter.ChapterFeedbackSetting
            {
                chapterName = name,
                feedbackLevel = existing.ContainsKey(name) ? existing[name] : 0
            });
        }

        EditorUtility.SetDirty(filter);
        Debug.Log($"[FeedbackChapterFilterEditor] Caricati {filter.chapterSettings.Count} capitoli.");
    }
}