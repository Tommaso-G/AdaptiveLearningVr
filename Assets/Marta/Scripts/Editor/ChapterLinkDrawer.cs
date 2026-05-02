using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomPropertyDrawer(typeof(ChapterLink))]
public class ChapterLinkDrawer : PropertyDrawer
{
    private static List<string> allChapters;
    private static List<string> optionalChapters;
    private static bool initialized = false;

    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    private void Init()
    {
        if (initialized) return;

        var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
        allChapters = data.chapters ?? new List<string>();

        optionalChapters = allChapters
            .Where(c => c.Contains("Optional"))
            .ToList();

        initialized = true;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init();

        EditorGUI.BeginProperty(position, label, property);

        var newChapterProp = property.FindPropertyRelative("newChapter");
        var previousChapterProp = property.FindPropertyRelative("previousChapter");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 4f;

        Rect newRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect prevRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

        if (allChapters == null || allChapters.Count == 0)
        {
            EditorGUI.LabelField(newRect, "No chapters found");
            EditorGUI.EndProperty();
            return;
        }

        DrawPopup(newRect, newChapterProp, "New Chapter", optionalChapters);
        DrawPopup(prevRect, previousChapterProp, "Previous Chapter", allChapters);

        EditorGUI.EndProperty();
    }

    private void DrawPopup(Rect rect, SerializedProperty property, string label, List<string> source)
    {
        if (source == null || source.Count == 0)
        {
            EditorGUI.LabelField(rect, label, "(none)");
            return;
        }

        int index = Mathf.Max(0, source.IndexOf(property.stringValue));
        int newIndex = EditorGUI.Popup(rect, label, index, source.ToArray());

        if (newIndex >= 0 && newIndex < source.Count)
        {
            property.stringValue = source[newIndex];
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 4f;

        return (lineHeight * 2) + spacing;
    }
}