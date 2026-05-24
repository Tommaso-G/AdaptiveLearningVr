// Metti questo file in una cartella Editor/ (es. Assets/Marta/Scripts/Editor/)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// ── Cache condivisa tra i drawer ──────────────────────────────────────────
internal static class OfflineLevelChapterCache
{
    public static List<string> AllChapters = new();
    public static List<string> OptionalChapters = new();
    private static bool _initialized = false;
    private static string _lastPath = null;

    private const string DefaultPath =
        "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public static void Init(string path = null)
    {
        path ??= DefaultPath;
        if (_initialized && path == _lastPath) return;

        var data = VRBuilderJsonReader.ParseProcessJson(path);
        AllChapters = data.chapters ?? new List<string>();
        OptionalChapters = AllChapters.Where(c => c.Contains("Optional")).ToList();
        _lastPath = path;
        _initialized = true;
    }

    public static void Invalidate() => _initialized = false;
}

// ── DRAWER: OptionalChapterEntry ──────────────────────────────────────────
[CustomPropertyDrawer(typeof(OptionalChapterEntry))]
public class OptionalChapterEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        OfflineLevelChapterCache.Init();
        var source = OfflineLevelChapterCache.OptionalChapters;

        EditorGUI.BeginProperty(position, label, property);

        var nameProp = property.FindPropertyRelative("chapterName");

        if (source == null || source.Count == 0)
        {
            EditorGUI.LabelField(position, label, new GUIContent("(nessun capitolo Optional trovato)"));
            EditorGUI.EndProperty();
            return;
        }

        int current = Mathf.Max(0, source.IndexOf(nameProp.stringValue));
        int selected = EditorGUI.Popup(position, label.text, current, source.ToArray());
        if (selected >= 0 && selected < source.Count)
            nameProp.stringValue = source[selected];

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}

// ── DRAWER: ChapterFeedbackOverride ───────────────────────────────────────
[CustomPropertyDrawer(typeof(ChapterFeedbackOverride))]
public class ChapterFeedbackOverrideDrawer : PropertyDrawer
{
    private static readonly string[] FeedbackLabels =
        { "0 – Feedback completo", "1 – Solo outline", "2 – Nessun feedback" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        OfflineLevelChapterCache.Init();
        var source = OfflineLevelChapterCache.AllChapters;

        EditorGUI.BeginProperty(position, label, property);

        float lh = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var chapterProp = property.FindPropertyRelative("chapterName");
        var feedbackProp = property.FindPropertyRelative("feedbackLevel");

        Rect row1 = new Rect(position.x, position.y, position.width, lh);
        Rect row2 = new Rect(position.x, position.y + lh + spacing, position.width, lh);

        if (source != null && source.Count > 0)
        {
            int ci = Mathf.Max(0, source.IndexOf(chapterProp.stringValue));
            int ni = EditorGUI.Popup(row1, "Capitolo", ci, source.ToArray());
            if (ni >= 0 && ni < source.Count)
                chapterProp.stringValue = source[ni];
        }
        else
        {
            EditorGUI.LabelField(row1, "Capitolo", "(nessun capitolo trovato)");
        }

        int fi = Mathf.Clamp(feedbackProp.intValue, 0, FeedbackLabels.Length - 1);
        int nfi = EditorGUI.Popup(row2, "Feedback Level", fi, FeedbackLabels);
        feedbackProp.intValue = Mathf.Clamp(nfi, 0, FeedbackLabels.Length - 1);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lh = EditorGUIUtility.singleLineHeight;
        return lh * 2 + 6f;
    }
}

// ── DRAWER: ChapterDifficultyOverride ─────────────────────────────────────
[CustomPropertyDrawer(typeof(ChapterDifficultyOverride))]
public class ChapterDifficultyOverrideDrawer : PropertyDrawer
{
    private static readonly string[] DifficultyLabels =
        { "0 – Base", "1 – Avanzato" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        OfflineLevelChapterCache.Init();
        var source = OfflineLevelChapterCache.AllChapters;

        EditorGUI.BeginProperty(position, label, property);

        float lh = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var chapterProp = property.FindPropertyRelative("chapterName");
        var difficultyProp = property.FindPropertyRelative("difficultyLevel");

        Rect row1 = new Rect(position.x, position.y, position.width, lh);
        Rect row2 = new Rect(position.x, position.y + lh + spacing, position.width, lh);

        if (source != null && source.Count > 0)
        {
            int ci = Mathf.Max(0, source.IndexOf(chapterProp.stringValue));
            int ni = EditorGUI.Popup(row1, "Capitolo", ci, source.ToArray());
            if (ni >= 0 && ni < source.Count)
                chapterProp.stringValue = source[ni];
        }
        else
        {
            EditorGUI.LabelField(row1, "Capitolo", "(nessun capitolo trovato)");
        }

        int di = Mathf.Clamp(difficultyProp.intValue, 0, DifficultyLabels.Length - 1);
        int ndi = EditorGUI.Popup(row2, "Difficulty Level", di, DifficultyLabels);
        difficultyProp.intValue = Mathf.Clamp(ndi, 0, DifficultyLabels.Length - 1);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lh = EditorGUIUtility.singleLineHeight;
        return lh * 2 + 6f;
    }
}