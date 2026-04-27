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

        if (feedbackHolder == null ||
            (feedbackHolder.FeedbackRepository == null && feedbackHolder.ProfilingFeedbackRepository == null))
        {
            Debug.LogError("[FeedbackAutoManager] Nessun FeedbackRepository assegnato.");
            return;
        }

        IEnumerable<FeedbackRepository.FeedbackData> feedbackList = null;
        LearningProfile profile = GetComponent<LearningProfile>();

        if (feedbackHolder.ProfilingFeedbackRepository != null)
        {
            Debug.Log("[FeedbackAutoManager] Uso ProfilingFeedbackRepository (profiling mode).");
            feedbackList = feedbackHolder.ProfilingFeedbackRepository.GetAllFeedbacks();
        }
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
                        {
                            RegisterTempoPreStep(stepName, feedback);
                            HandleStepCompletion(stepName);
                        }
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

                                    if (!feedbackMap.ContainsKey(subStepName))
                                        continue;

                                    var feedback = feedbackMap[subStepName];

                                    subStep.LifeCycle.StageChanged += (sender, args) =>
                                    {
                                        if (args.Stage == Stage.Activating)
                                            OnStepActivated(subStep, subChapterName, feedback);
                                        else if (args.Stage == Stage.Inactive)
                                        {
                                            RegisterTempoPreStep(subStepName, feedback);
                                            HandleStepCompletion(subStepName);
                                        }
                                    };

                                    totalStepCount++;
                                }
                            }

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

        string firstStep = feedback.StepForCompletition.FirstOrDefault();
        if (stepName != firstStep)
            return;

        if (shownFeedbacks.Contains(feedback))
            return;

        GameObject target = GetFirstGameObjectFromStep(step);

        if (target == null)
            return;

        List<Transform> feedbackPositions = feedbackDisplayer.FindFeedbackPositionChild(target);
        feedbackDisplayer.PrepareAndDisplayFeedback(feedback, feedbackPositions, feedbackHolder);

        // Segna il feedback come opzionale se il capitolo contiene "Optional",
        // prima che Start() venga eseguito così FeedbackPrefabController
        // istanzierà OptionalWayPoint invece di waypointPrefab
        GameObject instance = feedbackHolder.activeFeedbackInstance;
        if (instance != null)
        {
            FeedbackPrefabController controller = instance.GetComponent<FeedbackPrefabController>();
            if (controller != null)
                controller.isOptionalFeedback = chapterName.Contains("Optional");
        }

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
                remainingSteps.Remove(stepName);

            if (remainingSteps.Count == 0)
            {
                List<FeedbackPrefabController> prefabs = FindFeedbackInstance(feedback.FeedbackName);

                if (prefabs != null)
                {
                    for (int i = prefabs.Count - 1; i >= 0; i--)
                        prefabs[i].CloseFeedback();
                }

                feedbacksToRemove.Add(feedback);
            }
        }

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
            if (fb.name.Contains(feedbackName))
                feedbacksToRemove.Add(fb);
        }

        return feedbacksToRemove.Count() != 0 ? feedbacksToRemove : null;
    }

    private SlidesDataSender FindSender(string feedbackName)
    {
        var all = FindObjectsByType<SlidesDataSender>(FindObjectsSortMode.None);
        return all.FirstOrDefault(s => s != null && !string.IsNullOrEmpty(s.FeedbackName) && s.FeedbackName.Contains(feedbackName));
    }

    private void RegisterTempoPreStep(string stepName, FeedbackRepository.FeedbackData feedback)
    {
        string firstStep = feedback.StepForCompletition.FirstOrDefault();
        if (stepName != firstStep) return;

        var sender = FindSender(feedback.FeedbackName);
        if (sender != null)
        {
            float tempo = sender.GetCurrentTotalFocusTime();
            Debug.Log($"[RegisterTempoPreStep] Sender trovato per '{feedback.FeedbackName}', tempo: {tempo}");
            sender.SetTempoPreStep(tempo);
        }
        else
            Debug.LogWarning($"[FeedbackAutoManager] Nessun sender trovato per '{feedback.FeedbackName}'");
    }
}