using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRBuilder.Core;

[CustomPropertyDrawer(typeof(ChapterDropdownAttribute))]
public class ChapterDropdownDrawer : PropertyDrawer
{
    private List<string> allChapters = new();
    private bool initialized = false;

    // Default path
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";
    private string profilingPath = "Assets/StreamingAssets/Processes/Profiling/Profiling.json";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!initialized)
        {
            // Scegli il path in base al tipo dell'oggetto che contiene il campo
            var target = property.serializedObject.targetObject;

            string pathToUse = defaultPath;
            if (target is ProfilingFeedbackRepository)
                pathToUse = profilingPath;

            var data = VRBuilderJsonReader.ParseProcessJson(pathToUse);
            allChapters = data.chapters ?? new List<string>();
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
