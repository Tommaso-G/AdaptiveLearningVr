using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;

public class ChapterDropdownAttribute : PropertyAttribute
{
    // Solo un marker.
}

[CustomPropertyDrawer(typeof(ChapterDropdownAttribute))]
public class ChapterDropdownDrawer : PropertyDrawer
{
    private List<string> allChapters = new();
    private bool initialized = false;

    private const string ProcessJsonPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json"; // <-- aggiorna

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!initialized)
        {
            var data = VRBuilderJsonReader.ParseProcessJson(ProcessJsonPath);
            allChapters = data.chapters;
            initialized = true;
        }

        if (allChapters.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, "(nessun capitolo trovato)");
            return;
        }

        string current = property.stringValue;
        int currentIndex = Mathf.Max(0, allChapters.IndexOf(current));
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, allChapters.ToArray());

        if (newIndex >= 0 && newIndex < allChapters.Count && !string.IsNullOrEmpty(allChapters[newIndex]))
            property.stringValue = allChapters[newIndex];
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}
