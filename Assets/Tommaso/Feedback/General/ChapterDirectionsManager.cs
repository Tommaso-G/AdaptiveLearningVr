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
        public string messageSequenziale;
        [TextArea(2, 4)]
        public string messageGlobale;
    }

    [Header("Riferimenti")]
    public HandMenuRequester handMenuRequester;
    public TMP_Text textPanel;
    public LearningProfile learningProfile;
    public FeedbackChapterFilter chapterFilter; // ← aggiungi

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

        // Apre solo se il feedback level è 0
        if (chapterFilter != null && chapterFilter.GetFeedbackLevel(chapterName) != 0) return;

        ChapterDirectionEntry entry = chapterDirections.Find(e =>
            string.Equals(e.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (entry == null) return;

        bool isSequenziale = learningProfile == null ||
            learningProfile.GetProfileTuple().sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Sequenziale;

        string message = isSequenziale ? entry.messageSequenziale : entry.messageGlobale;

        if (string.IsNullOrEmpty(message)) return;

        if (textPanel != null)
            textPanel.text = message;

        handMenuRequester?.OpenMenu();
    }
}