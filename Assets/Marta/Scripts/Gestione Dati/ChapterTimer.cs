using System.Collections.Generic;
using UnityEngine;
using VRBuilder.Core;
using System;
using System.Linq;
using System.Collections;

public class ChapterTimer : MonoBehaviour
{
    [System.Serializable]
    public class ChapterTimerSettings
    {
        [Header("Capitolo che fa partire il timer")]
        [ChapterDropdown]
        public string chapterWithTimer;
        public float max_time = 150f;
        public bool timerOn = false;
        [Header("Mid timer event")]
        public bool useMidEvent = false;
        public float midEventTime = 60f;

        // ─────────────────────────────────────────────────────────────────
        // NUOVI CAMPI DI RUNTIME (Nascosti nell'Inspector)
        // ─────────────────────────────────────────────────────────────────
        [System.NonSerialized] public float elapsedTime = 0f;
        [System.NonSerialized] public Coroutine activeCoroutine = null;
        [System.NonSerialized] public bool isPaused = false;
        [System.NonSerialized] public bool midEventTriggered = false;
    }

    [SerializeField] ExecutionOrderController executionOrderController;
    [SerializeField] StepErrorTracker errorTracker;
    [SerializeField] FeedbackChapterFilter feedbackChapterFilter;
    public List<ChapterTimerSettings> timerSettings = new List<ChapterTimerSettings>();

    public event Action OnTimeExceeded;
    public event Action<string> OnMidEventTriggered;

    /// <summary>
    /// Avvia il timer o lo riprende (Resume) se era stato messo in pausa.
    /// </summary>
    public void StartTimer(string chapterName)
    {
        ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);

        if (currentChapter == null || currentChapter.timerOn)
            return;

        currentChapter.isPaused = false;
        currentChapter.activeCoroutine = StartCoroutine(TimerCoroutine(currentChapter));
    }

    private IEnumerator TimerCoroutine(ChapterTimerSettings currentChapter)
    {
        currentChapter.timerOn = true;

        print($"[ChapterTimer] Avviato/Ripreso timer per il capitolo {currentChapter.chapterWithTimer}. Tempo attuale: {currentChapter.elapsedTime:F1}/{currentChapter.max_time}s");

        while (currentChapter.elapsedTime < currentChapter.max_time)
        {
            currentChapter.elapsedTime += Time.deltaTime;

            // Mid-event: rende outline e waypoints visibili attraverso gli oggetti
            if (currentChapter.useMidEvent
                && !currentChapter.midEventTriggered
                && currentChapter.elapsedTime >= currentChapter.midEventTime)
            {
                currentChapter.midEventTriggered = true;
                TriggerMidEvent(currentChapter);
            }

            yield return null;
        }

        EndTimer(currentChapter);
    }

    private void TriggerMidEvent(ChapterTimerSettings currentChapter)
    {
        string chapterName = currentChapter.chapterWithTimer;

        print($"[ChapterTimer] Mid-event scattato per '{chapterName}' a {currentChapter.elapsedTime:F1}s");

        if (feedbackChapterFilter != null && currentChapter.useMidEvent)
        {
            feedbackChapterFilter.SetOutlineAlwaysVisibile(currentChapter.chapterWithTimer);
            print($"[ChapterTimer] FeedbackLevel '{chapterName}': outline sempre visibile");
        }
        else
        {
            Debug.LogWarning("[ChapterTimer] feedbackChapterFilter non assegnato, mid-event non può aggiornare il livello.");
        }

        if (currentChapter.useMidEvent)
        {
            OnMidEventTriggered?.Invoke(chapterName);
        }
    }

    public void DisableMidEventTrigger(string chapterWithMidEventToDisable)
    {
        ChapterTimerSettings chapterSetting = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterWithMidEventToDisable);
        chapterSetting.useMidEvent = false;
        Debug.Log($"[ChapterTimer] disabilitato Mid Event Trigger per il capitolo {chapterWithMidEventToDisable}.");
    }

    public void EndTimer(ChapterTimerSettings currentChapter, bool registerError = true)
    {
        if (currentChapter == null || !currentChapter.timerOn)
            return;

        print($"[ChapterTimer] Terminato il timer per il capitolo {currentChapter.chapterWithTimer} (Tempo Scaduto)");

        if ((errorTracker != null || executionOrderController != null) && registerError)
        {
            ErrorReporter errorReporter = new ErrorReporter();
            errorReporter.setReference(errorTracker, executionOrderController);

            string chapterErrorName = currentChapter.chapterWithTimer ?? "Unknown Chapter";

            errorReporter.chapterErrorName = chapterErrorName;
            errorReporter.RegisterError("ChapterTimer");
        }
        else
        {
            print($"[ChapterTimer] Impossibile segnare l'errore, Reference mancanti");
        }

        OnTimeExceeded?.Invoke();

        ResetChapterData(currentChapter);
    }

    /// <summary>
    /// Mette in pausa un singolo capitolo o TUTTI i capitoli attivi contemporaneamente.
    /// </summary>
    public void PauseTimer(string chapterName = "", bool all = false)
    {
        if (all)
        {
            foreach (ChapterTimerSettings cts in timerSettings)
            {
                // Se il timer non è attivo, lo saltiamo e passiamo al prossimo (CONTINUE, non return!)
                if (cts == null || !cts.timerOn)
                    continue;

                PauseChapterInternal(cts);
            }
        }
        else
        {
            ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);
            if (currentChapter != null && currentChapter.timerOn)
            {
                PauseChapterInternal(currentChapter);
            }
        }
    }

    /// <summary>
    /// Riprende il timer di un singolo capitolo o di TUTTI i capitoli precedentemente messi in pausa.
    /// </summary>
    public void ResumeTimer(string chapterName = "", bool all = false)
    {
        if (all)
        {
            foreach (ChapterTimerSettings cts in timerSettings)
            {
                // Riparte SOLO se era stato effettivamente messo in pausa ed è configurato correttamente
                if (cts == null || !cts.isPaused)
                    continue;

                ResumeChapterInternal(cts);
            }
        }
        else
        {
            ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);
            if (currentChapter != null && currentChapter.isPaused)
            {
                ResumeChapterInternal(currentChapter);
            }
        }
    }

    /// <summary>
    /// Ferma il timer definitivamente e AZZERA il tempo (es. quando il capitolo viene completato con successo).
    /// </summary>
    public void StopAndResetTimer(string chapterName)
    {
        ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);

        if (currentChapter == null)
            return;

        ResetChapterData(currentChapter);
        Debug.Log($"[ChapterTimer] Timer INTERROTTO e RESETTATO per {chapterName}");
    }

    public void EarlyEndChapterTimer(string chapterName)
    {
        ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);

        if (currentChapter == null)
            return;

        EarlyEndChapterTimerInteranal(currentChapter);
    }

    // ─────────────────────────────────────────────────────────────────
    // METODI INTERNI DI SUPPORTO
    // ─────────────────────────────────────────────────────────────────

    private void PauseChapterInternal(ChapterTimerSettings cts)
    {
        if (cts.activeCoroutine != null)
        {
            StopCoroutine(cts.activeCoroutine);
            cts.activeCoroutine = null;
        }

        cts.timerOn = false;
        cts.isPaused = true; // <--- Fondamentale per ricordarsi di lui al Resume globale
        Debug.Log($"[ChapterTimer] Timer in PAUSA per {cts.chapterWithTimer}. Tempo salvato: {cts.elapsedTime:F1}s");
    }

    private void ResumeChapterInternal(ChapterTimerSettings cts)
    {
        cts.isPaused = false;
        cts.activeCoroutine = StartCoroutine(TimerCoroutine(cts));
    }

    private void ResetChapterData(ChapterTimerSettings chapter)
    {
        if (chapter.activeCoroutine != null)
        {
            StopCoroutine(chapter.activeCoroutine);
            chapter.activeCoroutine = null;
        }
        chapter.timerOn = false;
        chapter.isPaused = false;
        chapter.elapsedTime = 0f;
        chapter.midEventTriggered = false;
    }

    private void EarlyEndChapterTimerInteranal(ChapterTimerSettings currentChapter)
    {
        EndTimer(currentChapter, registerError: false);
        Debug.Log($"[ChapterTimer] Capitolo {currentChapter.chapterWithTimer} saltato con EarlyEndChapterTimer");
    }
}