using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[CustomEditor(typeof(ChapterSkipHandler))]
public class ChapterSkipHandlerEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        ChapterSkipHandler handler = (ChapterSkipHandler)target;

        if (GUILayout.Button("Carica capitoli"))
        {
            LoadChapters(handler);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void LoadChapters(ChapterSkipHandler handler)
    {
        List<string> chaptersList = new List<string>();

        // PLAY MODE (VR Builder live)
        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process != null)
            {
                foreach (var chapter in process.Data.Chapters)
                    chaptersList.Add(chapter.Data.Name);
            }
        }
        // EDITOR MODE (JSON fallback)
        else
        {
            var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
            chaptersList = data.chapters;
        }

        if (chaptersList == null || chaptersList.Count == 0)
        {
            Debug.LogWarning("[ChapterSkipHandlerEditor] Nessun capitolo trovato.");
            return;
        }

        // salva eventi già assegnati
        var existingEvents = new Dictionary<string, UnityEvent>();

        foreach (var s in handler.chapterSettings)
        {
            existingEvents[s.chapterName] = s.onChapterSkipped;
        }

        handler.chapterSettings.Clear();

        // ricrea lista ordinata
        foreach (string name in chaptersList)
        {
            handler.chapterSettings.Add(new ChapterSkipHandler.ChapterSkipSetting
            {
                chapterName = name,
                onChapterSkipped = existingEvents.ContainsKey(name)
                    ? existingEvents[name]
                    : new UnityEvent()
            });
        }

        EditorUtility.SetDirty(handler);

        Debug.Log($"[ChapterSkipHandlerEditor] Caricati {handler.chapterSettings.Count} capitoli.");
    }
}