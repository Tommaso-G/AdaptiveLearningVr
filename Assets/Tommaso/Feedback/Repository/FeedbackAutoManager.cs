using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Core.SceneObjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using System;
using System.ComponentModel.Design;
using UnityEngine.UI;
using static FeedbackRepository;
using UnityEngine.Video;
using VRBuilder.Core.Behaviors;

public class FeedbackAutoManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public FeedbackSetHolder feedbackHolder;

    public FeedbackDisplayer feedbackDisplayer;

    public FeedbackChapterFilter chapterFilter;



    private Dictionary<FeedbackRepository.FeedbackData, HashSet<string>> activeFeedbackSteps = new();
    private HashSet<FeedbackRepository.FeedbackData> shownFeedbacks = new();

    private IProcess process;
    private void OnEnable()
    {
        if (ProcessRunner.Current != null)
        {
            process = ProcessRunner.Current;
            RegisterToStepEvents();
        }
        else
        {
            ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        }
    }

    private void OnDisable()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = args.Process;
        RegisterToStepEvents();
    }

    private void RegisterToStepEvents()
    {
        if (process == null || process.Data == null)
        {
            Debug.LogError("[FeedbackAutoManager] Nessun processo valido per la registrazione degli step.");
            return;
        }

        // Se non è assegnato né uno né l’altro, esci
        if (feedbackHolder == null ||
            (feedbackHolder.FeedbackRepository == null && feedbackHolder.ProfilingFeedbackRepository == null))
        {
            Debug.LogError("[FeedbackAutoManager] Nessun FeedbackRepository assegnato.");
            return;
        }

        IEnumerable<FeedbackRepository.FeedbackData> feedbackList = null;
        LearningProfile profile = GetComponent<LearningProfile>();

        // ==============================
        // Usa ProfilingFeedbackRepository se è presente
        // ==============================
        if (feedbackHolder.ProfilingFeedbackRepository != null)
        {
            Debug.Log("[FeedbackAutoManager] Uso ProfilingFeedbackRepository (profiling mode).");
            feedbackList = feedbackHolder.ProfilingFeedbackRepository.GetAllFeedbacks();
        }
        // ==============================
        // Altrimenti usa FeedbackRepository classico
        // ==============================
        else if (feedbackHolder.FeedbackRepository != null)
        {
            if (profile == null)
            {
                Debug.LogWarning("[FeedbackAutoManager] LearningProfile non trovato sul GameObject. Impossibile determinare il profilo per il repository standard.");
                return;
            }

            var profileTuple = profile.GetProfileTuple();
            feedbackList = feedbackHolder.FeedbackRepository.GetAllFeedbacksForProfile(profileTuple);
        }

        if (feedbackList == null)
        {
            Debug.LogWarning("[FeedbackAutoManager] Nessun feedback trovato nel repository selezionato.");
            return;
        }

        // ==============================
        // 3️⃣ Costruzione mappa step → feedback
        // ==============================
        var feedbackMap = new Dictionary<string, FeedbackRepository.FeedbackData>();
        foreach (var fb in feedbackList)
        {
            foreach (var stepName in fb.StepForCompletition)
            {
                if (!feedbackMap.ContainsKey(stepName))
                    feedbackMap[stepName] = fb;
            }
        }

        int mainChapterCount = 0;
        int subChapterCount = 0;
        int totalStepCount = 0;

        // ==============================
        // 4️⃣ Scansione dei capitoli principali
        // ==============================
        foreach (var chapter in process.Data.Chapters)
        {
            if (chapter == null)
                continue;

            mainChapterCount++;
            string chapterName = chapter.Data.Name;

            foreach (var stepChild in chapter.Data.Steps)
            {
                if (stepChild is IStep step)
                {
                    string stepName = step.Data.Name;
                    if (!feedbackMap.ContainsKey(stepName))
                        continue;

                    var feedback = feedbackMap[stepName];

                    step.LifeCycle.StageChanged += (sender, args) =>
                    {
                        if (args.Stage == Stage.Activating)
                            OnStepActivated(step, chapterName, feedback);
                        else if (args.Stage == Stage.Inactive)
                            HandleStepCompletion(stepName);
                    };

                    totalStepCount++;
                }
            }

            RegisterSubChaptersWithSeparateCount(chapter, feedbackMap, ref subChapterCount, ref totalStepCount);
        }

        Debug.Log($"[FeedbackAutoManager] Registrati {mainChapterCount} capitoli principali, {subChapterCount} sottocapitoli e {totalStepCount} step con feedback associato.");
    }




    private void RegisterSubChaptersWithSeparateCount(
        IChapter chapter,
        Dictionary<string, FeedbackRepository.FeedbackData> feedbackMap,
        ref int subChapterCount,
        ref int totalStepCount)
    {
        if (chapter?.Data?.Steps == null)
            return;

        foreach (var stepChild in chapter.Data.Steps)
        {
            if (stepChild is IStep step)
            {
                foreach (var behavior in step.Data.Behaviors.Data.Behaviors)
                {
                    if (behavior is ExecuteChaptersBehavior exec)
                    {
                        foreach (var sub in exec.Data.SubChapters)
                        {
                            if (sub?.Chapter == null)
                                continue;

                            var subChapter = sub.Chapter;
                            subChapterCount++;

                            foreach (var subStepChild in subChapter.Data.Steps)
                            {
                                if (subStepChild is IStep subStep)
                                {
                                    string subStepName = subStep.Data.Name;
                                    string subChapterName = subChapter.Data.Name;

                                    //Filtra solo gli step con feedback associato
                                    if (!feedbackMap.ContainsKey(subStepName))
                                        continue;

                                    var feedback = feedbackMap[subStepName];

                                    subStep.LifeCycle.StageChanged += (sender, args) =>
                                    {
                                        if (args.Stage == Stage.Activating)
                                            OnStepActivated(subStep, subChapterName, feedback);
                                        else if (args.Stage == Stage.Inactive)
                                            HandleStepCompletion(subStepName);
                                    };

                                    totalStepCount++;
                                }
                            }

                            //Ricorsione per sottocapitoli annidati
                            RegisterSubChaptersWithSeparateCount(subChapter, feedbackMap, ref subChapterCount, ref totalStepCount);
                        }
                    }
                }
            }
        }
    }




    private void OnStepActivated(IStep step, string chapterName, FeedbackRepository.FeedbackData feedback)
    {

        if (chapterFilter != null && !chapterFilter.IsFeedbackAllowed(chapterName))
        {
            Debug.Log($"[FeedbackAutoManager] Feedback disabilitato per '{chapterName}'.");
            return;
        }
        
        string stepName = step.Data.Name;

        // Mostra il feedback solo se è il primo step associato
        string firstStep = feedback.StepForCompletition.FirstOrDefault();
        if (stepName != firstStep)
        {
            //Debug.Log($"[FeedbackAutoManager] Step '{stepName}' attivato, ma non è il primo step per '{feedback.FeedbackName}', quindi non mostro il feedback.");
            return;
        }

        if (shownFeedbacks.Contains(feedback))
            return;

        // Usa direttamente lo step passato
        GameObject target = GetFirstGameObjectFromStep(step);

        if (target == null)
        {
            //Debug.LogWarning($"[FeedbackAutoManager] Nessun GameObject target trovato per '{stepName}' nel capitolo '{chapterName}'.");
            return;
        }

        List<Transform> feedbackPositions = feedbackDisplayer.FindFeedbackPositionChild(target);
        feedbackDisplayer.PrepareAndDisplayFeedback(feedback, feedbackPositions, feedbackHolder);

        if (!activeFeedbackSteps.ContainsKey(feedback))
            activeFeedbackSteps[feedback] = new HashSet<string>(feedback.StepForCompletition);

        shownFeedbacks.Add(feedback);
    }



    private void HandleStepCompletion(string stepName)
    {
        var feedbacksToRemove = new List<FeedbackRepository.FeedbackData>();

        foreach (var kvp in activeFeedbackSteps)
        {
            var feedback = kvp.Key;
            var remainingSteps = kvp.Value;

            if (remainingSteps.Contains(stepName))
            {
                remainingSteps.Remove(stepName);
                //Debug.Log($"[FeedbackAutoManager] Step '{stepName}' rimosso dai step rimanenti del feedback '{feedback.FeedbackName}'.");
            }

            // Se tutti gli step di questo feedback sono completati
            if (remainingSteps.Count == 0)
            {
                //Debug.Log($"[FeedbackAutoManager] Tutti gli step completati per '{feedback.FeedbackName}', chiudo il feedback.");

                // Trova il prefab del feedback attivo in scena
                List<FeedbackPrefabController> prefabs = FindFeedbackInstance(feedback.FeedbackName);

                if (prefabs != null)
                {
                    for (int i = prefabs.Count - 1; i >= 0; i--)
                    {
                        prefabs[i].CloseFeedback();
                    }
                    //Debug.Log($"[FeedbackAutoManager] Feedback prefab '{feedback.FeedbackName}' chiuso con animazione.");
                }
                else
                {
                    //Debug.LogWarning($"[FeedbackAutoManager] Nessuna istanza trovata per '{feedback.FeedbackName}'.");
                }

                feedbacksToRemove.Add(feedback);
            }
        }

        // Rimuovi i feedback completati dal dizionario
        foreach (var f in feedbacksToRemove)
        {
            activeFeedbackSteps.Remove(f);
            shownFeedbacks.Remove(f);
        }
    }

    private List<FeedbackPrefabController> FindFeedbackInstance(string feedbackName)
    {
        FeedbackPrefabController[] allFeedbacks = FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);

        List<FeedbackPrefabController> feedbacksToRemove = new List<FeedbackPrefabController>();

        foreach (var fb in allFeedbacks)
        {
            // Confronta i nomi, oppure puoi aggiungere un campo "feedbackName" al prefab per più robustezza
            if (fb.name.Contains(feedbackName))
            {
                feedbacksToRemove.Add(fb);
            }
        }

        return feedbacksToRemove.Count() != 0 ? feedbacksToRemove : null;

    }




}
