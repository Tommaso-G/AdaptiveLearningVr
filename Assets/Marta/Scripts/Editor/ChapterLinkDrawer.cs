using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(ChapterLink))]
public class ChapterLinkDrawer : PropertyDrawer
{
    private string defaultPath = "Assets/StreamingAssets/Processes/Extinguisher/Extinguisher.json";
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Trova lo script principale (quello che contiene OptionalChapters e nodes)
        var target = property.serializedObject.targetObject as ChaptersOrderManager;

        if (target == null)
        {
            EditorGUI.LabelField(position, "ChaptersOrderManager not found");
            return;
        }

        var newChapterProp = property.FindPropertyRelative("newChapter"); // cerca proprietà newChapter di ChapterLink
        var previousChapterProp = property.FindPropertyRelative("previousChapter");// cerca proprietà previusChapter di ChapterLink

        // crea le due colonne per i drawer
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        float halfWidth = position.width / 2;

        // colonne base
        Rect leftColumn = new Rect(position.x, position.y, halfWidth - 5, position.height);
        Rect rightColumn = new Rect(position.x + halfWidth + 5, position.y, halfWidth - 5, position.height);

        // titoli
        Rect leftTitleRect = new Rect(leftColumn.x, leftColumn.y, leftColumn.width, lineHeight);
        Rect rightTitleRect = new Rect(rightColumn.x, rightColumn.y, rightColumn.width, lineHeight);

        // popup sotto il titolo
        Rect leftRect = new Rect(leftColumn.x, leftColumn.y + lineHeight + spacing, leftColumn.width, lineHeight);
        Rect rightRect = new Rect(rightColumn.x, rightColumn.y + lineHeight + spacing, rightColumn.width, lineHeight);

        // disegna titoli
        EditorGUI.LabelField(leftTitleRect, "New Chapter", EditorStyles.boldLabel);
        EditorGUI.LabelField(rightTitleRect, "Previous Chapter", EditorStyles.boldLabel);

        var optionalChaptersList = new List<string>(); ;
        var previousChaptersList = new List<string>(); ;
        string[] optionalChapters = null; 
        string[] previousChapters = null;

        if (Application.isPlaying)
        {
            if (target.EditorChaptersReady)
            {
                optionalChaptersList = new List<string>(target.OptionalChapters);

                if (!optionalChaptersList.Contains("None"))
                    optionalChaptersList.Add("None");

                optionalChapters = optionalChaptersList.ToArray();

                optionalChapters = optionalChaptersList.ToArray();
                previousChaptersList = target.AvailablePreviousChapters.ToList();
                previousChapters = previousChaptersList.ToArray();
            }
        }
        else
        {
            string pathToUse = defaultPath;
            var data = VRBuilderJsonReader.ParseProcessJson(pathToUse);
            previousChaptersList = data.chapters;
            previousChapters = previousChaptersList.ToArray();

            foreach (string chapter in previousChaptersList)
            {
                if (chapter.Contains("Optional"))
                {
                    optionalChaptersList.Add(chapter);
                }
            }

            optionalChapters = optionalChaptersList.ToArray();
        }

        if (optionalChapters == null || previousChapters == null) return;

        int newIndex = System.Array.IndexOf(optionalChapters, newChapterProp.stringValue); // cerca indice corrispondente a newChapter scelto

        if (newIndex < 0)
        {
            newIndex = 0;
            if (optionalChapters.Length > 0)
                newChapterProp.stringValue = optionalChapters[0];
        }

        newIndex = EditorGUI.Popup(leftRect, newIndex, optionalChapters);

        if (optionalChaptersList.Count > 0)
            if (optionalChapters[newIndex] == "None")
            {
                newChapterProp.stringValue = "";
            }
            else
            {
                newChapterProp.stringValue = optionalChapters[newIndex];
            }

        int prevIndex = System.Array.IndexOf(previousChapters, previousChapterProp.stringValue);

        if (prevIndex < 0)
        {
            prevIndex = 0;
            if (previousChapters.Length > 0)
                previousChapterProp.stringValue = previousChapters[0];
        }

        prevIndex = EditorGUI.Popup(rightRect, prevIndex, previousChapters);

        if (previousChaptersList.Count > 0)
        {
            if (previousChapters[prevIndex] == "None")
                previousChapterProp.stringValue = "";
            else
                previousChapterProp.stringValue = previousChapters[prevIndex];
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        return (lineHeight * 2) + spacing;
    }
}