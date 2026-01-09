using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;


public class StepForCompletionDropdownAttribute : PropertyAttribute
{
    // Solo un marker.
}

[CustomPropertyDrawer(typeof(StepForCompletionDropdownAttribute))]
public class StepForCompletionDrawer : PropertyDrawer
{
    private List<string> allSteps = new();
    private bool initialized = false;

    private const string ProcessJsonPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json"; // <-- aggiorna

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!initialized)
        {
            var data = VRBuilderJsonReader.ParseProcessJson(ProcessJsonPath);
            allSteps = data.steps;
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

