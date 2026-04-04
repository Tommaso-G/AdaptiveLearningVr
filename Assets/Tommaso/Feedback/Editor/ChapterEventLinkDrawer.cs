using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(OptionalChapterEventTrigger.ChapterEventLink))]
public class ChapterEventLinkDrawer : PropertyDrawer
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var chapterNameProp = property.FindPropertyRelative("chapterName");
        var onChapterActiveProp = property.FindPropertyRelative("onChapterActive");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        // Titolo dropdown
        Rect titleRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.LabelField(titleRect, "Optional Chapter", EditorStyles.boldLabel);

        // Dropdown capitoli opzionali
        Rect popupRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

        List<string> optionalChapters = new List<string>();

        if (Application.isPlaying)
        {
            var process = VRBuilder.Core.ProcessRunner.Current;
            if (process != null)
                foreach (var chapter in process.Data.Chapters)
                    if (chapter.Data.Name.Contains("Optional"))
                        optionalChapters.Add(chapter.Data.Name);
        }
        else
        {
            var data = VRBuilderJsonReader.ParseProcessJson(defaultPath);
            foreach (var name in data.chapters)
                if (name.Contains("Optional"))
                    optionalChapters.Add(name);
        }

        if (optionalChapters.Count == 0)
        {
            EditorGUI.LabelField(popupRect, "Nessun capitolo opzionale trovato");
        }
        else
        {
            string[] chaptersArray = optionalChapters.ToArray();
            int currentIndex = optionalChapters.IndexOf(chapterNameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            currentIndex = EditorGUI.Popup(popupRect, currentIndex, chaptersArray);
            chapterNameProp.stringValue = chaptersArray[currentIndex];
        }

        // Evento
        float eventHeight = EditorGUI.GetPropertyHeight(onChapterActiveProp);
        Rect eventRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, eventHeight);
        EditorGUI.PropertyField(eventRect, onChapterActiveProp, new GUIContent("On Chapter Active"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var onChapterActiveProp = property.FindPropertyRelative("onChapterActive");
        float eventHeight = EditorGUI.GetPropertyHeight(onChapterActiveProp);

        return (lineHeight + spacing) * 2 + eventHeight + spacing;
    }
}