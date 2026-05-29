using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ChapterDirectionsManager))]
public class ChapterDirectionsManagerEditor : Editor
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnInspectorGUI()
    {
        ChapterDirectionsManager manager = (ChapterDirectionsManager)target;

        manager.handMenuRequester = (HandMenuRequester)EditorGUILayout.ObjectField(
            "Hand Menu Requester", manager.handMenuRequester, typeof(HandMenuRequester), true);

        manager.textPanel = (TMPro.TMP_Text)EditorGUILayout.ObjectField(
            "Text Panel", manager.textPanel, typeof(TMPro.TMP_Text), true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Percorso JSON processo", EditorStyles.boldLabel);
        defaultPath = EditorGUILayout.TextField(defaultPath);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Carica capitoli", GUILayout.Height(28)))
            LoadChapters(manager);

        EditorGUILayout.Space(12);

        // Tabella capitolo → messaggio
        EditorGUILayout.LabelField("Capitolo → Messaggio", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        foreach (var entry in manager.chapterDirections)
        {
            EditorGUILayout.LabelField(entry.chapterName, EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            string newMessage = EditorGUILayout.TextArea(entry.message, GUILayout.MinHeight(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Modifica messaggio capitolo");
                entry.message = newMessage;
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.Space(4);
        }
    }

    private void LoadChapters(ChapterDirectionsManager manager)
    {
        List<string> chapters = new List<string>();

        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process == null)
            {
                Debug.LogWarning("[ChapterDirectionsManagerEditor] Nessun processo in esecuzione.");
                return;
            }

            foreach (var chapter in process.Data.Chapters)
                chapters.Add(chapter.Data.Name);
        }
        else
        {
            var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
            chapters = data.chapters;
        }

        if (chapters == null || chapters.Count == 0)
        {
            Debug.LogWarning("[ChapterDirectionsManagerEditor] Nessun capitolo trovato.");
            return;
        }

        // Preserva i messaggi già inseriti
        var existing = new Dictionary<string, string>();
        foreach (var entry in manager.chapterDirections)
            existing[entry.chapterName] = entry.message;

        manager.chapterDirections.Clear();

        foreach (string name in chapters)
        {
            existing.TryGetValue(name, out string savedMessage);
            manager.chapterDirections.Add(new ChapterDirectionsManager.ChapterDirectionEntry
            {
                chapterName = name,
                message     = savedMessage ?? string.Empty
            });
        }

        EditorUtility.SetDirty(manager);
        Debug.Log($"[ChapterDirectionsManagerEditor] Caricati {manager.chapterDirections.Count} capitoli.");
    }
}
