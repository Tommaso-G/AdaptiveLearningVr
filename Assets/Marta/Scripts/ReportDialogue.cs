using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public struct DialogueLine
{
    [TextArea(2, 5)]
    public string text;
    [Header("present students")]
    public int present;
    [Header("evacuated students")]
    public int evacuated;
    [Header("missing students")]
    public int missing;
}

[CreateAssetMenu(fileName = "ReportDialogue", menuName = "Scriptable Objects/ReportDialogue")]
public class ReportDialogue : ScriptableObject
{
    public List<DialogueLine> dialogueOptions;
}
