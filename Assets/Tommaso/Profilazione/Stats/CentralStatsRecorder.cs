using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static VisivoVerbaleStatManager;

public class CentralStatsRecorder : MonoBehaviour
{

    public float Weight_OpeningOrder = 0.5f;

    public float Weight_TimeOfFocus = 0.3f;

    public float Weight_OpeningTimes = 0.2f;


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
        public float scoreGlobaleSequenziale; // 0 = tutto sequenziale, 1 = tutto globale
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



    public LearningSessionSlidesData CalcolaMediaSlidesData()
    {
        var result = new LearningSessionSlidesData();
        var feedbacks = slidesDataRecorder.GetAllFeedbacks().ToList();

        if (feedbacks.Count == 0) return result;

        float totaleScore = 0f;
        float totalePreStep = 0f;
        float totaleOpeningSequenziale = 0f;
        float totaleOpeningGlobale = 0f;

        foreach (var feedback in feedbacks)
        {
            float score = CalcolaScoreFeedback(feedback);
            totaleScore += score;

            var slideSeq = feedback.slidesData.Values
                .Where(s => s.seqGlob == LearningEnums.SequenzialeGlobale.Sequenziale)
                .ToList();
            var slideGlob = feedback.slidesData.Values
                .Where(s => s.seqGlob == LearningEnums.SequenzialeGlobale.Globale)
                .ToList();

            totaleOpeningSequenziale += slideSeq.Count > 0 ? (float)slideSeq.Average(s => s.opening) : 0f;
            totaleOpeningGlobale += slideGlob.Count > 0 ? (float)slideGlob.Average(s => s.opening) : 0f;
            totalePreStep += feedback.tempoOsservazionePreStep;

            Debug.Log($"[CentralStatsRecorder] Feedback: {feedback.feedbackName} → score globale: {score:F2}");
        }

        int count = feedbacks.Count;
        result.scoreGlobaleSequenziale = totaleScore / count; // 0=seq, 1=glob
        result.mediaTempoPreStep = totalePreStep / count;

        Debug.Log($"[CentralStatsRecorder] Score globale/sequenziale medio: {result.scoreGlobaleSequenziale:F2}");
        Debug.Log($"[CentralStatsRecorder] Media tempo pre-step: {result.mediaTempoPreStep:F2}s");


        return result;
    }
    private float CalcolaScoreFeedback(FeedbackDataContainer feedback)
    {
        var slideSeq = feedback.slidesData.Values
            .Where(s => s.seqGlob == LearningEnums.SequenzialeGlobale.Sequenziale && !s.isIntroductory)
            .ToList();

        var slideGlob = feedback.slidesData.Values
            .Where(s => s.seqGlob == LearningEnums.SequenzialeGlobale.Globale && !s.isIntroductory)
            .ToList();

        //ScoreOrdine
        float scoreOrdine = 0.5f;
        var visitHistory = feedback.visitHistory;
        if (visitHistory != null && visitHistory.Count >= 1)
        {
            // rimuovi duplicati mantenendo l'ordine (prime aperture)
            var primeAperture = visitHistory
                .Distinct()
                .ToList();

        if (primeAperture.Count >= 1)
        {
            var sequenzaCompleta = new List<int> { 0 }; // parte sempre da 0
            sequenzaCompleta.AddRange(primeAperture);

            float avgJump = 0f;
            for (int i = 1; i < sequenzaCompleta.Count; i++)
                avgJump += Mathf.Abs(sequenzaCompleta[i] - sequenzaCompleta[i - 1]);
            avgJump /= (sequenzaCompleta.Count - 1);

            scoreOrdine = Mathf.Clamp((avgJump - 1) / 2f, 0f, 1f);
        }
        }

        // 2. ScoreTempo
        float tempoGlob = slideGlob.Count > 0 ? slideGlob.Average(s =>
            s.visVerb == LearningEnums.VisivoVerbale.Verbale ? s.normalizedFocusTime : s.focusTime) : 0f;

        float tempoSeq = slideSeq.Count > 0 ? slideSeq.Average(s =>
            s.visVerb == LearningEnums.VisivoVerbale.Verbale ? s.normalizedFocusTime : s.focusTime) : 0f;

        float totTempo = tempoGlob + tempoSeq;
        float scoreTempo = totTempo > 0 ? (float)(tempoGlob / totTempo) : 0.5f;

        // 3. ScoreOpening
        float openingSeq = slideSeq.Count > 0 ? (float)slideSeq.Average(s => s.opening) : 0f;
        float openingGlob = slideGlob.Count > 0 ? (float)slideGlob.Average(s => s.opening) : 0f;

        float totOpening = openingSeq + openingGlob;
        float scoreOpening = totOpening > 0 ? 1f - (float)(openingSeq / totOpening) : 0.5f;

        // 4. Score finale
        float score = Weight_OpeningOrder * scoreOrdine + Weight_TimeOfFocus* scoreTempo + Weight_OpeningTimes * scoreOpening;

        Debug.Log($"[CalcolaScoreFeedback] {feedback.feedbackName} → " +
                $"ScoreOrdine: {scoreOrdine:F2} | ScoreTempo: {scoreTempo:F2} | ScoreOpening: {scoreOpening:F2} | Score: {score:F2}");

        return score;
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
        string path = System.IO.Path.Combine("C:/Users/utente/OneDrive/Documenti/Tesi/Profilazione/Dati", "ProfilingSessionData.json");
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[CentralStatsRecorder] File salvato in: {path}");
    }

    public ProfilingSessionData CalcolaStatisticheFinali()
    {
        var result = new ProfilingSessionData();

        // inizializza container
        result.genericData = new GenericSessionData();

        // STOP + READ TIMER
        if (sessionTimer != null)
        {
            sessionTimer.StopTimer();
            result.genericData.sessionTimeSeconds = sessionTimer.GetTime();
        }

        // statistiche esistenti
        result.slidesData = CalcolaMediaSlidesData();
        result.visivoVerbaleData = CalcolaMediaGiochi();
        result.assemblyData = GetAssemblyData();
        result.minigamesData = GetMinigamesData(); 


        // somma tutti i tempi totali di osservazione dei feedback
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
                    visVerb = slide.visVerb.ToString()
                });
            }

            data.feedbacks.Add(entry);
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string path = System.IO.Path.Combine("C:/Users/utente/OneDrive/Documenti/Tesi/Profilazione/Dati", "FeedbacksSessionData.json");
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[CentralStatsRecorder] Feedback salvati in: {path}");
    }

    

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            CalcolaStatisticheFinali();
        }
    }
}