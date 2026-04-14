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
        public LearningSessionSlidesData slidesData;
        public VisivoVerbaleSessionData visivoVerbaleData;
        public AssemblySessionData assemblyData;
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
    }

    [System.Serializable]
    public class GameVisivoVerbaleData
    {
        public string gameID;
        public List<ModalitaEntry> modalita = new List<ModalitaEntry>();
    }

    [System.Serializable]
    public class VisivoVerbaleSessionData
    {
        public List<GameVisivoVerbaleData> giochi = new List<GameVisivoVerbaleData>();
    }

    [SerializeField] private SlidesDataRecorder slidesDataRecorder;
    [SerializeField] private VisivoVerbaleStatManager visivoVerbaleStatManager;
    [SerializeField] private AssemblyManager assemblyManager;

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
                gameData.modalita.Add(new ModalitaEntry
                {
                    modalita = modalita.Key.ToString(),
                    tempoTotale = modalita.Value.tempoTotale,
                    erroriTotali = modalita.Value.erroriTotali
                });

                Debug.Log($"[CentralStatsRecorder] Gioco: {gioco.Key} | " +
                          $"Modalità: {modalita.Key} | " +
                          $"Tempo totale: {modalita.Value.tempoTotale:F2}s | " +
                          $"Errori: {modalita.Value.erroriTotali}");
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
        string path = System.IO.Path.Combine(Application.persistentDataPath, "ProfilingSessionData.json");
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[CentralStatsRecorder] File salvato in: {path}");
    }

    public ProfilingSessionData CalcolaStatisticheFinali()
    {
        var result = new ProfilingSessionData();

        result.slidesData = CalcolaMediaSlidesData();
        result.visivoVerbaleData = CalcolaMediaGiochi();
        result.assemblyData = GetAssemblyData();

        SalvaJson(result);

        Debug.Log("[CentralStatsRecorder] CalcolaStatisticheFinali completato.");

        return result;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            CalcolaStatisticheFinali();
        }
    }
}