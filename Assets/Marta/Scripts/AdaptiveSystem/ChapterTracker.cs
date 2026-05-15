// ChapterTracker.cs - attaccato al GameObject del capitolo in VRBuilder
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;

public class ChapterTracker : MonoBehaviour
{

    private string currentChapterName = null;
    private string chapterIdToRegister = null;
    [SerializeField] private StepErrorTracker stepErrorTracker = null;
    [SerializeField] private ChapterTimer timer;

    private int _errorCount = 0;
    private float _startTime;

    private float _timeToRegister = 0;
    private int _errorToRegister = 0;

    private int currentIdx = -1;
    private int idxToRegister = -1;

    private IProcess process;
    private List<string> chaptersToExclude = new List<string>();

    private ChaptersOrderManager co_mgr;

    private bool ChapterSkipped = false;

    private bool waitForData = false;
    private List<(string, float)> timeChanges = new List<(string, float)>();

    public Action<string, string, int, float> ObservationDataReady;
    public static System.Action<string, float> ChangeTime;

    private Dictionary<string, bool> subChapterTimerReady = new();
    private void Start()
    {
        co_mgr = FindAnyObjectByType<ChaptersOrderManager>();
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
        co_mgr.OnRemoveCurrent += HandleChapterSkipped;
        ChangeTime += HandleTimeChange;
        timer = FindAnyObjectByType<ChapterTimer>();
    }
    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
        Debug.Log($"[ChapterTracker] Processo preso");
    }

    public void setChaptersToExclude(List<ChapterConfigData> chapters)
    {
        Debug.Log($"[ChapterTracker] settati capitoli da escludere");
        foreach (ChapterConfigData chapter in chapters)
        {
            chaptersToExclude.Add(chapter.name);
        }
    }

    public void OnChapterStarted(object sender, ProcessEventArgs args)
    {   // registra gli errori del capitolo precedente se presenti
        Debug.Log($"[ChapterTracker] Iniziato un capitolo");
        if (currentChapterName != null)
        {
            chapterIdToRegister = currentChapterName;
            idxToRegister = currentIdx;
            _timeToRegister = (Time.time - _startTime);
            timer?.StopTimer(chapterIdToRegister);
            OnChapterComplete(chapterIdToRegister, idxToRegister, _timeToRegister);
        }

        currentChapterName = process.Data.Current.Data.Name;
        timer?.StartTimer(currentChapterName);
        currentIdx = process.Data.Chapters.IndexOf(process.Data.Current);
        _errorCount = 0;
        _startTime = Time.time;

        Debug.Log($"[ChapterTracker] Capitolo {currentChapterName} iniziato");

        if (co_mgr.ChapterWithExecuteBehavior.TryGetValue(currentChapterName, out ExecuteChaptersBehavior executeChaptersBehavior))
        {
            Debug.Log($"[ChapterTracker] Capitolo {currentChapterName} contiene un esecuzione parallela");

            if (executeChaptersBehavior.Data.addedSubChapter > 0)
            {
                foreach (SubChapter sc in executeChaptersBehavior.Data.AddedSubChapters)
                {
                    StartCoroutine(SubChapterTracker(sc));
                }
            }
        }
    }

    public IEnumerator SubChapterTracker(SubChapter subChapter)
    {
        IChapter chapter = subChapter.Chapter;
        string subChapterName = chapter.Data.Name;
        int subIdx = process.Data.Chapters.IndexOf(chapter);
        int errorCount = 0;

        subChapterTimerReady[subChapterName] = false;

        Debug.Log($"[ChapterTracker] Sottocapitolo {subChapterName} creato, in attesa di attivazione.");
        
        // aspetta che il sottocapitolo sia effettivamente iniziato
        yield return new WaitUntil(() =>
            subChapterTimerReady.ContainsKey(subChapterName) &&
            subChapterTimerReady[subChapterName]
        );

        float startTime = Time.time;
        //timer?.StartTimer(subChapterName);

        Debug.Log($"[ChapterTracker] Tracking del sottocapitolo {chapter.Data.Name} iniziato");

        // Aspetta che il sottocapiotlo sia concluso
        while (chapter.LifeCycle.Stage != Stage.Active)
        {
            yield return null;
        }

        // Rimuovi la chiave dal dizionario
        if (subChapterTimerReady.ContainsKey(subChapterName))
        {
            subChapterTimerReady.Remove(subChapterName);
        }

        //timer?.StopTimer(subChapterName);
        Debug.Log($"[ChapterTracker] Tracking del sottocapitolo {chapter.Data.Name} concluso");

        if (subChapter.IsOptional)
        {
            Debug.Log($"[ChapterTracker] Sottocapitolo {chapter.Data.Name} è stato rimosso, i dati non verranno inviati alla rete.");
            yield break;
        }

        float timeToRegister = Time.time - startTime;

        if(timeToRegister < 0.5f)
        {
            timeToRegister = 500f;
        }
        OnChapterComplete(subChapterName, subIdx, timeToRegister);

    }

    public void StartTimerForSubChapter(string chapterName)
    {
        if (subChapterTimerReady.ContainsKey(chapterName))
        {
            subChapterTimerReady[chapterName] = true;
        }
        else
        {
            Debug.Log($"[ChapterTracker] il Sottocapitolo {chapterName} non è nel dizionario");
        }
    }

    // ── Chiamato da VRBuilder quando il capitolo finisce ─────────────────
    public void OnChapterComplete(string chapter_name, int chapter_idx, float time)
    {
        if (chaptersToExclude.Contains(chapter_name))
        {
            return;
        }

        int errors = 0;

        if (stepErrorTracker.ChapterErrors.TryGetValue(chapter_name, out var error))
        {
            errors = error.TotalErrors;
        }
        else
        {
            Debug.Log($"[ChapterTracker] Capitolo {chapter_name} non presente in ChapterErrors di StepErrorTracker");
            return;
        }

        if (!ChapterSkipped)
        {

            StartCoroutine(prepareData(chapter_idx, time, errors));
        }
        else
        {
            print("[ChapterTracker] il capitolo è stato saltato perchè rimosso");
            ChapterSkipped = false;
        }
    }

    private IEnumerator prepareData(int chapter_idx, float time, int errors)
    {
        while (waitForData)
        {
            yield return null;
        }

        string chapter_name = process.Data.Chapters[chapter_idx].Data.Name;

        var element = timeChanges.FirstOrDefault(e => e.Item1 == chapter_name);
        float time_change = element.Item1 != null ? element.Item2 : 0f;

        ObservationDataReady?.Invoke(
            process.Data.Chapters[chapter_idx].ChapterMetadata.Guid.ToString(),
            chapter_name,
            errors,
            time + time_change);

        Debug.Log($"[ChapterTracker] Capitolo {chapter_name} completato. " +
            $"Errori: {errors}, Tempo: {time + time_change:F2} sec");
    }
    private void HandleTimeChange(string chapter_name, float timeDelta)
    {
        waitForData = true;
        timeChanges.Add((chapter_name, timeDelta));
        waitForData = false;
        Debug.Log($"[ChapterTracker] Aggiunti {timeDelta:F2} sec al tempo del capitolo {chapter_name}.");
    }

    private void HandleChapterSkipped()
    {
        ChapterSkipped = true;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
        ChangeTime += HandleTimeChange;
    }
}