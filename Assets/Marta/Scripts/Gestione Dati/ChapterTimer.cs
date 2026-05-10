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
    }

    [SerializeField] ExecutionOrderController executionOrderController;
    [SerializeField] StepErrorTracker errorTracker;
    public List<ChapterTimerSettings> timerSettings = new List<ChapterTimerSettings>();
    public event Action OnTimeExceeded;
    public void StartTimer(string chapterName)
    {
        ChapterTimerSettings currentChapter = timerSettings.FirstOrDefault(cs => cs.chapterWithTimer == chapterName);

        if (currentChapter == null || currentChapter.timerOn)
            return;

        StartCoroutine(TimerCoroutine(currentChapter));

    }

    private IEnumerator TimerCoroutine(ChapterTimerSettings currentChapter)
    {
        currentChapter.timerOn = true;

        print($"[ChapterTimer] Lanciato il timer per il capitolo {currentChapter.chapterWithTimer}");

        float elapsed_time = 0f;

        while (elapsed_time < currentChapter.max_time)
        {
            elapsed_time += Time.deltaTime;
            yield return null;   // ← aspetta il frame successivo
        }

        EndTimer(currentChapter);
    }

    public void EndTimer(ChapterTimerSettings currentChapter)
    {
        if (currentChapter == null || !currentChapter.timerOn)
            return;

        print($"[ChapterTimer] Terminato il timer per il capitolo {currentChapter.chapterWithTimer}");

        if(errorTracker != null || executionOrderController != null)
        {
            ErrorReporter errorReporter = new ErrorReporter();
            errorReporter.setReference(errorTracker, executionOrderController);

            string chapterErrorName;

            if (currentChapter.chapterWithTimer == null)
            {
                chapterErrorName = "Unknown Chapter";
            }
            else
            {
                chapterErrorName = currentChapter.chapterWithTimer;
            }

            errorReporter.chapterErrorName = chapterErrorName;
            errorReporter.RegisterError("ChapterTimer");
        }
        else
        {
            print($"[ChapterTimer] Impossibile segnar l'errore, Reference mancanti");
        }

        OnTimeExceeded?.Invoke();
        currentChapter.timerOn = false;
    }

    public void StopTimer(string chapterName)
    {
        ChapterTimerSettings currentChapter = timerSettings
            .FirstOrDefault(cs => cs.chapterWithTimer == chapterName);

        if (currentChapter == null || !currentChapter.timerOn)
            return;

        StopAllCoroutines(); // oppure tieni traccia della coroutine specifica
        currentChapter.timerOn = false;
        Debug.Log($"[ChapterTimer] Timer fermato per {chapterName}");
    }
}
