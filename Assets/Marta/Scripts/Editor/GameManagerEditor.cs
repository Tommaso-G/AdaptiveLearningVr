using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        GameManager manager = (GameManager)target;

        if (GUILayout.Button("Carica Capitoli dal Process"))
        {
            LoadChapters(manager);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void LoadChapters(GameManager manager)
    {
        string path = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

        var data = VRBuilderJsonReader.ParseChaptersWithGuid(path);

        if (data == null)
        {
            Debug.LogWarning("Nessun capitolo trovato nel JSON.");
            return;
        }

        manager.allChapters.Clear();

        foreach (var chapter in data)
        {
            manager.allChapters.Add(new ChapterConfigData
            {
                chapter_id = chapter.guid,
                name = chapter.name,
                is_mandatory = chapter.name.Contains("Optional")? false : true, // default, poi lo cambi tu
                max_errors = 10,
                min_time_sec = 60f,
                max_time_sec = 300f
            });
        }

        EditorUtility.SetDirty(manager);
    }
}