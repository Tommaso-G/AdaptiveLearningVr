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

        manager.learningProfile = (LearningProfile)EditorGUILayout.ObjectField(
            "Learning Profile", manager.learningProfile, typeof(LearningProfile), true);

        manager.chapterFilter = (FeedbackChapterFilter)EditorGUILayout.ObjectField(
            "Chapter Filter", manager.chapterFilter, typeof(FeedbackChapterFilter), true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Percorso JSON processo", EditorStyles.boldLabel);
        defaultPath = EditorGUILayout.TextField(defaultPath);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Carica capitoli", GUILayout.Height(28)))
            LoadChapters(manager);

        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField("Capitolo → Messaggio", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        foreach (var entry in manager.chapterDirections)
        {
            EditorGUILayout.LabelField(entry.chapterName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Sequenziale
            EditorGUILayout.LabelField("Sequenziale", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string newSeq = EditorGUILayout.TextArea(entry.messageSequenziale, GUILayout.MinHeight(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Modifica messaggio sequenziale");
                entry.messageSequenziale = newSeq;
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.Space(2);

            // Globale
            EditorGUILayout.LabelField("Globale", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string newGlob = EditorGUILayout.TextArea(entry.messageGlobale, GUILayout.MinHeight(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Modifica messaggio globale");
                entry.messageGlobale = newGlob;
                EditorUtility.SetDirty(manager);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
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
        var existingSeq  = new Dictionary<string, string>();
        var existingGlob = new Dictionary<string, string>();
        foreach (var entry in manager.chapterDirections)
        {
            existingSeq[entry.chapterName]  = entry.messageSequenziale;
            existingGlob[entry.chapterName] = entry.messageGlobale;
        }

        manager.chapterDirections.Clear();

        foreach (string name in chapters)
        {
            existingSeq.TryGetValue(name,  out string savedSeq);
            existingGlob.TryGetValue(name, out string savedGlob);

            manager.chapterDirections.Add(new ChapterDirectionsManager.ChapterDirectionEntry
            {
                chapterName        = name,
                messageSequenziale = savedSeq  ?? string.Empty,
                messageGlobale     = savedGlob ?? string.Empty
            });
        }

        EditorUtility.SetDirty(manager);
        Debug.Log($"[ChapterDirectionsManagerEditor] Caricati {manager.chapterDirections.Count} capitoli.");
    }
}