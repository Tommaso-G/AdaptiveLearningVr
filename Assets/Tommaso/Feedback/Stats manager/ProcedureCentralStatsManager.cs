using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProcedureCentralStatsManager : MonoBehaviour
{
    [System.Serializable]
    public class ProfilingSessionData
    {
        public GenericSessionData genericData;
        public LearningSessionSlidesData slidesData;
        public FeedbacksSessionData feedbacksData;
    }

    [System.Serializable]
    public class GenericSessionData
    {
        public float sessionTimeSeconds;
        public float feedbackTimeSeconds;
    }

    [System.Serializable]
    public class LearningSessionSlidesData
    {
        public float mediaTempoPreStep;
    }

    [System.Serializable]
    public class SlideEntry
    {
        public string pageName;
        public float focusTime;
        public float normalizedFocusTime;
        public int opening;
        public int videoButtonClicks;
    }

    [System.Serializable]
    public class FeedbackEntry
    {
        public string feedbackName;
        public float tempoOsservazionePreStep;
        public float tempoTotaleOsservazione;
        public List<int> visitHistory;
        public List<SlideEntry> slides = new List<SlideEntry>();
    }

    [System.Serializable]
    public class FeedbacksSessionData
    {
        public List<FeedbackEntry> feedbacks = new List<FeedbackEntry>();
    }

    [SerializeField] private SlidesDataRecorder slidesDataRecorder;
    [SerializeField] private EventTimer sessionTimer;
    [SerializeField] private List<string> feedbackDaEscludere = new List<string>();

    private string profilingSessionName = "Nome";

    void Start()
    {
        if (PlayerPrefs.HasKey("ProfilingSessionName"))
            profilingSessionName = PlayerPrefs.GetString("ProfilingSessionName");
    }

    private string GetFolderPath()
    {
        string customPath = PlayerPrefs.GetString("ProfilingSessionPath", "");
        if (!string.IsNullOrEmpty(customPath))
            return customPath;

        return System.IO.Path.Combine(Application.persistentDataPath, "Tommaso", "Profilazione", "Dati");
    }

    public LearningSessionSlidesData CalcolaMediaSlidesData()
    {
        var result = new LearningSessionSlidesData();
        var feedbacks = slidesDataRecorder.GetAllFeedbacks().ToList();

        if (feedbacks.Count == 0)
            return result;

        float sommaPesataPreStep = 0f;
        float sommaPesi = 0f;

        for (int i = 0; i < feedbacks.Count; i++)
        {
            float t = feedbacks.Count == 1 ? 1f : (float)i / (feedbacks.Count - 1);
            float peso = 0.5f + Mathf.Pow(t, 2f);
            sommaPesataPreStep += feedbacks[i].tempoOsservazionePreStep * peso;
            sommaPesi += peso;
        }

        result.mediaTempoPreStep = sommaPesataPreStep / sommaPesi;
        Debug.Log($"[ProcedureCentralStatsManager] Tempo pre-step medio pesato: {result.mediaTempoPreStep:F2}s");
        return result;
    }

    public FeedbacksSessionData GetFeedbacksData()
    {
        Debug.Log($"[GetFeedbacksData] Feedback nel recorder: {slidesDataRecorder.GetAllFeedbacks().Count()}");
        foreach (var f in slidesDataRecorder.GetAllFeedbacks())
        Debug.Log($"[GetFeedbacksData] Feedback: '{f.feedbackName}' | slides: {f.slidesData?.Count} | escluso: {feedbackDaEscludere.Contains(f.feedbackName)}");
        
        var data = new FeedbacksSessionData();

        foreach (var feedback in slidesDataRecorder.GetAllFeedbacks())
        {
            if (feedbackDaEscludere.Contains(feedback.feedbackName)) continue;

            var entry = new FeedbackEntry
            {
                feedbackName = feedback.feedbackName,
                tempoOsservazionePreStep = feedback.tempoOsservazionePreStep,
                tempoTotaleOsservazione = feedback.tempoTotaleOsservazione,
                visitHistory = feedback.visitHistory
            };

            foreach (var slide in feedback.slidesData.Values)
            {
                entry.slides.Add(new SlideEntry
                {
                    pageName = slide.pageName,
                    focusTime = slide.focusTime,
                    normalizedFocusTime = slide.normalizedFocusTime,
                    opening = slide.opening,
                    videoButtonClicks = slide.VideobuttonClicks
                });
            }

            data.feedbacks.Add(entry);
        }

        return data;
    }

    public void CalcolaStatisticheFinali()
    {
        var result = new ProfilingSessionData();

        result.genericData = new GenericSessionData();

        if (sessionTimer != null)
        {
            sessionTimer.StopTimer();
            result.genericData.sessionTimeSeconds = sessionTimer.GetTime();
        }

        result.slidesData = CalcolaMediaSlidesData();
        result.feedbacksData = GetFeedbacksData();

        result.genericData.feedbackTimeSeconds = slidesDataRecorder.GetAllFeedbacks()
            .Sum(f => f.tempoTotaleOsservazione);

        SalvaJson(result);
        Debug.Log($"[ProcedureCentralStatsManager] Sessione chiusa. Tempo: {result.genericData.sessionTimeSeconds:F2}s");
        //return result;
    }

    public void SalvaJson(ProfilingSessionData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string folderPath = GetFolderPath();

        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        string filePath = System.IO.Path.Combine(folderPath, $"{profilingSessionName}ProcedureSessionData.json");
        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"[ProcedureCentralStatsManager] File salvato in: {filePath}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            CalcolaStatisticheFinali();
    }
}