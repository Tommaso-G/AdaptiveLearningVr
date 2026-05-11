using System.Linq;
using UnityEngine;
using VRBuilder.Core;
using System.Collections.Generic;
using VRBuilder.Core.Entities.Factories;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;
using System;
using VRBuilder.Core.Configuration;
using static UnityEngine.Rendering.GPUSort;
using JetBrains.Annotations;
using UnityEngine.Rendering;
using VRBuilder.Core.Behaviors;
using System.Collections;


public enum AddChapter
{
    Disattivo = 0,
    Attivo = 1,
    Completato = 2
}
public class EditProcess : MonoBehaviour
{
    [SerializeField] private AddChapter addChapter;
    [SerializeField] private ChaptersOrderManager co_mgr; // gestisce la lista di nodi che rappresentano i capitoli

    private IProcess process;
    private IChapter chapter;
    public int lastChapterId { get; private set; }

    bool skipCurrent = false;

    [SerializeField] private ChapterSkipHandler skipHandler;
    [SerializeField] private ChapterTimer chapterTimer;

    private void Start()
    {
        UnityEngine.Debug.Log("Inizio modifica processo...");
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
        ProcessRunner.Events.StepStarted += OnStepStarted;
        co_mgr.OnListChanged += CheckNextChapter;
        co_mgr.OnRemoveCurrent += skipCurrentChapter;
        co_mgr.OnRemoveSubChapter += removedSubChapter;
        chapterTimer = FindAnyObjectByType<ChapterTimer>();
        if (chapterTimer != null)
        {
            chapterTimer.OnTimeExceeded += skipCurrentChapter;
        }
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Processo iniziato!");
        process = ProcessRunner.Current;
        co_mgr.initialize(process);
        lastChapterId = process.Data.Chapters.Count - co_mgr.OptionalChapters.Count - 1;
        //process = GlobalEditorHandler.GetCurrentProcess(); // per vedere nell'editor cosa succede (non cancellare)
    }

    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Capitolo iniziato: " + args.Process.Data.Current.Data.Name);

        // se il capitolo � opzionale e non stai aggiungendo capitoli, saltalo
        if (args.Process.Data.Current.Data.Name.Contains("Optional") && addChapter != AddChapter.Attivo)
        {
            UnityEngine.Debug.Log("Capitolo opzionale");
            disableChapters(args.Process.Data.Current.Data.Name);
        }

        //if (addCondition)
        //{
        //    chapter = process.Data.Chapters.FirstOrDefault(c => c.Data.Name == chapterToEdit);
        //}
    }

    private void CheckNextChapter()
    {
        if (process.Data.Chapters.IndexOf(process.Data.Current) == lastChapterId)
        {
            addChapter = AddChapter.Completato;
        }

        if (addChapter == AddChapter.Attivo)
        {
            setNextChapter(co_mgr.head);
            UnityEngine.Debug.Log("Chiamata di setNextChapter");
        }
    }
    private void OnStepStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Step iniziato: " + args.Process.Data.Current.Data.Current.Data.Name);

        //if (addCondition)
        //{
        //    IStep step = chapter.Data.Steps.Last();
        //    newCondition(step);
        //}

        //if (addStep)
        //{
        //    IStep step = EntityFactory.CreateStep("New Step");
        //    newStep(step);
        //}
    }

    private void disableChapters(string chapter)
    {
        ProcessRunner.SkipChapters(co_mgr.OptionalChapters.Count - co_mgr.OptionalChapters.IndexOf(chapter)); // salta i capitoli opzionali rimasti
    }
    public void skipCurrentChapter()
    {
        StartCoroutine(skipCurrentCoroutine());
    }

    public IEnumerator skipCurrentCoroutine()
    {
        while (process?.Data.Current?.Data.Current?.LifeCycle.Stage != Stage.Active &&
       process?.Data.Current?.Data.Current?.LifeCycle.Stage != Stage.Activating)
        {
            yield return null;
        }

        string currentName = process.Data.Current.Data.Name;

        bool skipped = false;
        bool stepGroup = false;
        // Completa prima i nested chapters se presenti
        yield return StartCoroutine(CompleteNestedChapters(process.Data.Current, result => stepGroup = result));

        if (stepGroup)
        {
            Debug.Log($"[EDITPROCESS] stepGroup trovato e saltato.");
            yield break;
        }

        try
        {
            ProcessRunner.SkipChapters(1);
            skipped = true;
        }
        catch (Exception e)
        {
            // Fallback: forza il completamento ignorando errori di FastForward
            Debug.LogWarning($"[EDITPROCESS] Errore durante skip: {e.Message}. Tentativo fallback.");
        }

        skipHandler.NotifyChapterSkipped(currentName);

        if (!skipped)
        {
            Debug.LogError("[EDITPROCESS] Skip fallito, il capitolo potrebbe non essere avanzato.");
        }
        else
        {
            Debug.Log($"[EDITPROCESS] current chapter skipped ({currentName})");
        }
    }

    private IEnumerator CompleteNestedChapters(IChapter chapter, Action<bool> onCompleted)
    {

        Debug.Log($"[EDITPROCESS] CompleteNestedChapters chiamato sul capitolo {chapter.Data.Name}");

        bool stepGroup = false;
        IStep currentStep = chapter.Data.Current;

        if (currentStep == null)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        Debug.Log($"[EDITPROCESS] Step corrente {currentStep.Data.Name}");
        foreach (var behavior in currentStep.Data.Behaviors.Data.Behaviors)
        {
            if (behavior is ExecuteChaptersBehavior chapterBehavior)
            {
                Debug.Log("[EDITPROCESS] Trovati ExecuteChapterBehavior");
                List<SubChapter> nestedChapter = chapterBehavior.Data.SubChapters;

                foreach (var subChapter in nestedChapter)
                {
                    if (subChapter != null && subChapter.Chapter.LifeCycle.Stage == Stage.Activating)
                    {
                        //Ricorsione per StepGroup annidati a più livelli
                        //yield return StartCoroutine(CompleteNestedChapters(nestedChapter));

                        subChapter.Chapter.LifeCycle.MarkToFastForwardStage(Stage.Activating);
                        removedSubChapter(subChapter.Chapter.Data.Name);
                        Debug.Log($"[EDITPROCESS] sottocapitolo {subChapter.Chapter.Data.Name} MarkToFastForward. Stage: {subChapter.Chapter.LifeCycle.Stage}");
                        //yield return new WaitUntil(() =>
                        //    nestedChapter.LifeCycle.Stage == Stage.Inactive ||
                        //    nestedChapter.LifeCycle.Stage == Stage.Deactivating);
                    }
                }

                stepGroup = true;
                onCompleted?.Invoke(stepGroup);

            }
            else
            {
                Debug.Log("[EDITPROCESS] NON trovati ExecuteChapterBehavior");

            }
        }
    }

    public void removedSubChapter(string subChapterName)
    {
        skipHandler.NotifyChapterSkipped(subChapterName);
        UnityEngine.Debug.Log($"[EDITPROCESS] subchapter {subChapterName} skipped");
    }

    private void setNextChapter(Node currentNode)
    {
        if (process != null && co_mgr != null)
        {
            if (co_mgr.empty) // empty � vero quando leggo l'ultimo nodo
            {
                addChapter = AddChapter.Completato;
                ProcessRunner.SkipChapters(currentNode.chapterId);
                //UnityEngine.Debug.Log("AddChapter: " + addChapter);
            }
            else
            {
                addChapter = AddChapter.Attivo;

                Node optionalNext = currentNode.OptionalNext;
                if (optionalNext != null)
                {
                    IChapter chapter = process.Data.Chapters[optionalNext.chapterId];
                    ProcessRunner.SetNextChapter(chapter); // override del capitolo successivo
                    UnityEngine.Debug.Log("Next chapter is: " + chapter.Data.Name);
                }
            }
        }
        else
        {
            UnityEngine.Debug.Log("Reference null");
        }
    }
    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
        ProcessRunner.Events.StepStarted -= OnStepStarted;
        co_mgr.OnListChanged -= CheckNextChapter;
        co_mgr.OnRemoveCurrent -= skipCurrentChapter;
    }
}
