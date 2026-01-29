using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRBuilder.Core;

[CustomPropertyDrawer(typeof(StepForCompletionDropdownAttribute))]
public class StepForCompletionDrawer : PropertyDrawer
{
    private List<string> allSteps = new();
    private bool initialized = false;

    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";
    private string profilingPath = "Assets/StreamingAssets/Processes/Profiling/Profiling.json";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!initialized)
        {
            // Determina il percorso in base all'oggetto che contiene il campo
            var target = property.serializedObject.targetObject;
            string pathToUse = defaultPath;

            if (target is ProfilingFeedbackRepository)
                pathToUse = profilingPath;

            var data = VRBuilderJsonReader.ParseProcessJson(pathToUse);
            allSteps = data.steps ?? new List<string>();
            initialized = true;
        }

        if (allSteps.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, "(nessuno step trovato)");
            return;
        }

        string current = property.stringValue;
        int currentIndex = Mathf.Max(0, allSteps.IndexOf(current));
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, allSteps.ToArray());

        if (newIndex >= 0 && newIndex < allSteps.Count && !allSteps[newIndex].StartsWith("---"))
            property.stringValue = allSteps[newIndex];
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}
