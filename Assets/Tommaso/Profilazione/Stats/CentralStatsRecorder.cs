using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static VisivoVerbaleStatManager;

public class CentralStatsRecorder : MonoBehaviour
{

    [System.Serializable]
    public class ProfilingSessionData
    {
        public GenericSessionData genericData;  
        public LearningSessionSlidesData slidesData;
        public VisivoVerbaleSessionData visivoVerbaleData;
        public AssemblySessionData assemblyData;
        public MinigamesSessionData minigamesData; 
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
    public class AssemblySessionData
    {
        public float punteggioSequenziale;
        public float punteggioGlobale;
        public float tempoTotaleSecondi;
        public int riaperturePannello;
    }

    [System.Serializable]
    public class ModalitaEntry
    {
        public string modalita;
        public float tempoTotale;
        public int erroriTotali;
        public List<ParametroExtraEntry> parametriExtra = new List<ParametroExtraEntry>();
    }

    [System.Serializable]
    public class ParametroExtraEntry
    {
        public string nome;
        public float valore;
    }

    [System.Serializable]
    public class GameVisivoVerbaleData
    {
        public string gameID;
        public List<ModalitaEntry> modalita = new List<ModalitaEntry>();
    }

    [System.Serializable]
    public class MinigameEntry
    {
        public string minigameChapterName;
        public float completitionTime;
        public int errors;
        public int moves;
    }

    [System.Serializable]
    public class MinigamesSessionData
    {
        public List<MinigameEntry> minigames = new List<MinigameEntry>();
    }

    [System.Serializable]
    public class VisivoVerbaleSessionData
    {
        public List<GameVisivoVerbaleData> giochi = new List<GameVisivoVerbaleData>();
    }

    [System.Serializable]
    public class SlideEntry
    {
        public string pageName;
        public float focusTime;
        public float normalizedFocusTime;
        public int opening;
        public string seqGlob;
        public string visVerb;
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
    [SerializeField] private VisivoVerbaleStatManager visivoVerbaleStatManager;
    [SerializeField] private AssemblyManager assemblyManager;
    [SerializeField] private EventTimer sessionTimer;
    [SerializeField] private MinigameDataRecorder minigameDataRecorder;
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
            var feedback = feedbacks[i];

            float t = (feedbacks.Count == 1)
                ? 1f
                : (float)i / (feedbacks.Count - 1);

            float peso = 0.5f + Mathf.Pow(t, 2f);

            sommaPesataPreStep += feedback.tempoOsservazionePreStep * peso;
            sommaPesi += peso;
        }

        result.mediaTempoPreStep = sommaPesataPreStep / sommaPesi;

        Debug.Log($"[CentralStatsRecorder] Tempo pre-step medio pesato: {result.mediaTempoPreStep:F2}s");

        return result;
    }

    public VisivoVerbaleSessionData CalcolaMediaGiochi()
    {
        var storico = visivoVerbaleStatManager.GetStorico();
        var sessionData = new VisivoVerbaleSessionData();

        foreach (var gioco in storico)
        {
            var gameData = new GameVisivoVerbaleData();
            gameData.gameID = gioco.Key;

            foreach (var modalita in gioco.Value)
            {
                var entry = new ModalitaEntry
                {
                    modalita = modalita.Key.ToString(),
                    tempoTotale = modalita.Value.tempoTotale,
                    erroriTotali = modalita.Value.erroriTotali
                };

                foreach (var kv in modalita.Value.parametriExtra)
                {
                    entry.parametriExtra.Add(new ParametroExtraEntry
                    {
                        nome = kv.Key,
                        valore = kv.Value
                    });
                }

                gameData.modalita.Add(entry);
            }

            sessionData.giochi.Add(gameData);
        }

        return sessionData;
    }

    public AssemblySessionData GetAssemblyData()
    {
        return new AssemblySessionData
        {
            punteggioSequenziale = assemblyManager.PunteggioSequenziale,
            punteggioGlobale = assemblyManager.PunteggioGlobale,
            tempoTotaleSecondi = assemblyManager.GetTempoTotale(),
            riaperturePannello = assemblyManager.GetRiaperturePannello()
        };
    }

    public void SalvaJson(ProfilingSessionData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);

        string folderPath = GetFolderPath();

        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        string filePath = System.IO.Path.Combine(folderPath, $"{profilingSessionName}ProfileSessionData.json");

        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"[CentralStatsRecorder] File salvato in: {filePath}");
    }

    public ProfilingSessionData CalcolaStatisticheFinali()
    {
        var result = new ProfilingSessionData();

        result.genericData = new GenericSessionData();

        if (sessionTimer != null)
        {
            sessionTimer.StopTimer();
            result.genericData.sessionTimeSeconds = sessionTimer.GetTime();
        }

        result.slidesData = CalcolaMediaSlidesData();
        result.visivoVerbaleData = CalcolaMediaGiochi();
        result.assemblyData = GetAssemblyData();
        result.minigamesData = GetMinigamesData();

        result.genericData.feedbackTimeSeconds = slidesDataRecorder.GetAllFeedbacks()
            .Sum(f => f.tempoTotaleOsservazione);

        SalvaJson(result);
        SalvaJsonFeedbacks();
        Debug.Log($"[CentralStatsRecorder] Sessione chiusa. Tempo: {result.genericData.sessionTimeSeconds:F2}s");

        return result;
    }

    public MinigamesSessionData GetMinigamesData()
    {
        var result = new MinigamesSessionData();

        foreach (var kvp in minigameDataRecorder.minigameDataList)
        {
            result.minigames.Add(new MinigameEntry
            {
                minigameChapterName = kvp.Value.minigameChapterName,
                completitionTime = kvp.Value.completitionTime,
                errors = kvp.Value.errors,
                moves = kvp.Value.moves
            });
        }

        return result;
    }

    public void SalvaJsonFeedbacks()
    {
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
                    seqGlob = slide.seqGlob.ToString(),
                    visVerb = slide.visVerb.ToString(),
                    videoButtonClicks = slide.VideobuttonClicks
                });
            }

            data.feedbacks.Add(entry);
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);

        string folderPath = GetFolderPath();

        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        string filePath = System.IO.Path.Combine(folderPath, $"{profilingSessionName}FeedbacksSessionData.json");
        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"[CentralStatsRecorder] Feedback salvati in: {filePath}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            CalcolaStatisticheFinali();
        }
    }
}