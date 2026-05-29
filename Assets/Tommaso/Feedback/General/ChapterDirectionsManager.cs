using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRBuilder.Core;

public class ChapterDirectionsManager : MonoBehaviour
{
    [System.Serializable]
    public class ChapterDirectionEntry
    {
        [HideInInspector]
        public string chapterName;
        [TextArea(2, 4)]
        public string message;
    }

    [Header("Riferimenti")]
    public HandMenuRequester handMenuRequester;
    public TMP_Text textPanel;

    [Header("Mappa capitolo → messaggio")]
    public List<ChapterDirectionEntry> chapterDirections = new List<ChapterDirectionEntry>();

    private void Start()
    {
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
    }

    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        string chapterName = ProcessRunner.Current?.Data?.Current?.Data?.Name ?? "";
        if (string.IsNullOrEmpty(chapterName)) return;

        ChapterDirectionEntry entry = chapterDirections.Find(e =>
            string.Equals(e.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (entry == null || string.IsNullOrEmpty(entry.message)) return;

        if (textPanel != null)
            textPanel.text = entry.message;

        handMenuRequester?.OpenMenu();
    }
}
