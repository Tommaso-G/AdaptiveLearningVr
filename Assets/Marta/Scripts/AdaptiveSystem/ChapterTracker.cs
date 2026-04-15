// ChapterTracker.cs - attaccato al GameObject del capitolo in VRBuilder
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core;

public class ChapterTracker : MonoBehaviour
{

    private string currentChapterName = null;
    private string chapterIdToRegister = null;
    [SerializeField] private StepErrorTracker stepErrorTracker = null;

    private int _errorCount = 0;
    private float _startTime;

    private float _timeToRegister = 0;
    private int _errorToRegister = 0;

    private int currentIdx = -1;
    private int idxToRegister = -1;

    private IProcess process;
    private List<string> chaptersToExclude = new List<string>();


    private void Start()
    {
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
    }
    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
    }

    public void setChaptersToExclude(List<ChapterConfigData> chapters)
    {
        foreach (ChapterConfigData chapter in chapters)
        {
            chaptersToExclude.Add(chapter.name);
        }
    }

    public void OnChapterStarted(object sender, ProcessEventArgs args)
    {   // registra gli errori del capitolo precedente se presente
        if (currentChapterName != null)
        {
            chapterIdToRegister = currentChapterName;
            idxToRegister = currentIdx;
            _timeToRegister = (Time.time - _startTime);
            OnChapterComplete();
        }
        currentChapterName = process.Data.Current.Data.Name;
        currentIdx = process.Data.Chapters.IndexOf(process.Data.Current);
        _errorCount = 0;
        _startTime = Time.time;
        Debug.Log($"[ChapterTracker] Capitolo {currentChapterName} iniziato");
    }

    // ── Chiamato da VRBuilder quando il capitolo finisce ─────────────────
    public void OnChapterComplete()
    {
        if (chaptersToExclude.Contains(chapterIdToRegister))
        {
            return;
        }

        if (stepErrorTracker.ChapterErrors.TryGetValue(chapterIdToRegister, out var error))
        {
            _errorToRegister = error.TotalErrors;
        }
        else
        {
            Debug.Log($"[ChapterTracker] Capitolo {chapterIdToRegister} non presente in ChapterErrors di StepErrorTracker");
            return;
        }

        Debug.Log($"[ChapterTracker] Capitolo {chapterIdToRegister} completato. " +
                    $"Errori: {_errorToRegister}, Tempo: {_timeToRegister:F2} min");

        // Invia al sistema adattivo e gestisci la risposta
        AdaptiveSystemClient.Instance.SendObservation(
            chapterId: process.Data.Chapters[idxToRegister].ChapterMetadata.Guid.ToString(),
            chapterName: process.Data.Chapters[idxToRegister].Data.Name,
            errors: _errorToRegister,
            timeSec: _timeToRegister
        );
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
    }
}