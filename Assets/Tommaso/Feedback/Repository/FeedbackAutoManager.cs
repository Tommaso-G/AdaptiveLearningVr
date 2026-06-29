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
    public ChapterTimer chapterTimer;

    [Header("Hard Assist (Outline e icone sempre visibili")]
    [Tooltip("OutlineManager della scena")]
    public StepOutlineManager outlineManager;
    [Tooltip("Layer usato dai waypoint in condizioni normali (es. 'UI' o 'Waypoint')")]
    public string normalWaypointLayer = "UI";
    [Tooltip("Layer assegnato ai waypoint quando scatta Hard Assist")]
    public string hardAssistWaypointLayer = "Default";

    [Header("Impostazioni Delay")]
    [Tooltip("Secondi di attesa tra chiusura e apertura di un feedback sulla stessa posizione")]
    public float feedbackPositionDelay = 3f;

    private Dictionary<FeedbackRepository.FeedbackData, (HashSet<string> steps, string chapterName)> activeFeedbackSteps = new();
    private HashSet<FeedbackRepository.FeedbackData> shownFeedbacks = new();

    private Dictionary<Vector3Int, float> positionCooldowns = new();

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

        if (chapterTimer != null)
            chapterTimer.OnMidEventTriggered += OnChapterMidEvent;
    }

    private void OnDisable()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;

        if (chapterTimer != null)
            chapterTimer.OnMidEventTriggered -= OnChapterMidEvent;
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
                Debug.LogWarning("[FeedbackAutoManager] LearningProfile non trovato sul GameObject.");
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
            if (chapter == null) continue;

            mainChapterCount++;
            string chapterName = chapter.Data.Name;

            foreach (var stepChild in chapter.Data.Steps)
            {
                if (stepChild is IStep step)
                {
                    string stepName = step.Data.Name;
                    if (!feedbackMap.ContainsKey(stepName)) continue;

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
        if (chapter?.Data?.Steps == null) return;

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
                            if (sub?.Chapter == null) continue;

                            var subChapter = sub.Chapter;
                            subChapterCount++;

                            foreach (var subStepChild in subChapter.Data.Steps)
                            {
                                if (subStepChild is IStep subStep)
                                {
                                    string subStepName = subStep.Data.Name;
                                    string subChapterName = subChapter.Data.Name;

                                    if (!feedbackMap.ContainsKey(subStepName)) continue;

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

    private Vector3Int PositionKey(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x * 10),
            Mathf.RoundToInt(worldPos.y * 10),
            Mathf.RoundToInt(worldPos.z * 10)
        );
    }

    private void OnStepActivated(IStep step, string chapterName, FeedbackRepository.FeedbackData feedback)
    {
        if (feedback.StepForCompletition != null && feedback.StepForCompletition.Count > 0)
        {
            Debug.Log("[FeedbackAutoManager] StepForCompletition trovati:");
            foreach (string s in feedback.StepForCompletition)
                Debug.Log($" - {s}");
        }
        else
        {
            Debug.Log("[FeedbackAutoManager] Nessuno StepForCompletition trovato.");
        }

        Debug.Log($"[FeedbackAutoManager] OnStepActivated per '{step.Data.Name}'.");

        if (chapterFilter != null && !chapterFilter.IsFeedbackAllowed(chapterName))
        {
            Debug.Log($"[FeedbackAutoManager] Feedback disabilitato per '{chapterName}'.");
            return;
        }

        string stepName = step.Data.Name;
        string firstStep = feedback.StepForCompletition.FirstOrDefault();

        if (stepName != firstStep)
        {
            Debug.Log($"[FeedbackAutoManager] QUIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII.");
            return;
        }

        if (shownFeedbacks.Contains(feedback))
            return;

        GameObject target = GetFirstGameObjectFromStep(step);
        if (target == null) return;

        List<Transform> feedbackPositions = feedbackDisplayer.FindFeedbackPositionChild(target);
        if (feedbackPositions == null) return;

        float delay = 0f;
        foreach (var pos in feedbackPositions)
        {
            Vector3Int key = PositionKey(pos.position);
            if (positionCooldowns.TryGetValue(key, out float until))
            {
                float remaining = until - Time.time;
                if (remaining > delay)
                    delay = remaining;
            }
        }

        shownFeedbacks.Add(feedback);

        if (!activeFeedbackSteps.ContainsKey(feedback))
            activeFeedbackSteps[feedback] =
                (new HashSet<string>(feedback.StepForCompletition), chapterName);

        StartCoroutine(ShowFeedbackAfterDelay(delay, chapterName, feedback, feedbackPositions));

        Debug.Log($"[FeedbackAutoManager] feedback mostrato");
    }

    private IEnumerator ShowFeedbackAfterDelay(
        float delay,
        string chapterName,
        FeedbackRepository.FeedbackData feedback,
        List<Transform> feedbackPositions)
    {
        if (delay > 0f)
        {
            Debug.Log($"[FeedbackAutoManager] Attendo {delay:F2}s per '{feedback.FeedbackName}' (posizione in cooldown).");
            yield return new WaitForSeconds(delay);
        }

        if (!shownFeedbacks.Contains(feedback))
        {
            Debug.Log($"[FeedbackAutoManager] booooooooooooooooooooooooooooooooo");
            yield break;
        }

        Debug.Log($"[FeedbackAutoManager] {feedback} {feedbackPositions} {feedbackHolder}");

        feedbackDisplayer.PrepareAndDisplayFeedback(feedback, feedbackPositions, feedbackHolder);

        GameObject instance = feedbackHolder.activeFeedbackInstance;
        if (instance != null)
        {
            FeedbackPrefabController controller = instance.GetComponent<FeedbackPrefabController>();
            if (controller != null)
                controller.isOptionalFeedback = chapterName.Contains("Optional");
        }
    }

    public Coroutine RunCoroutineSafe(IEnumerator routine)
    {
        if (routine == null) return null;
        return StartCoroutine(routine);
    }

    public void StopCoroutineSafe(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    private void HandleStepCompletion(string stepName)
    {
        var feedbacksToRemove = new List<FeedbackRepository.FeedbackData>();
    
        foreach (var kvp in activeFeedbackSteps)
        {
            var feedback       = kvp.Key;
            var remainingSteps = kvp.Value.steps;
            var chapterName    = kvp.Value.chapterName;
    
            if (remainingSteps.Contains(stepName))
                remainingSteps.Remove(stepName);
    
            if (remainingSteps.Count == 0)
            {
                List<FeedbackPrefabController> prefabs = FindFeedbackInstance(feedback.FeedbackName);
    
                // ← ADATTIVO
                if (OfflineAdaptiveController.Instance != null && prefabs != null && prefabs.Count > 0)
                {
                    SlidesDataSender sender = FindSender(feedback.FeedbackName);
                    OfflineAdaptiveController.Instance.OnFeedbackCompleted(sender, prefabs[0], chapterName);
                }
    
                if (prefabs != null)
                {
                    for (int i = prefabs.Count - 1; i >= 0; i--)
                    {
                        Vector3Int key = PositionKey(prefabs[i].transform.position);
                        positionCooldowns[key] = Time.time + feedbackPositionDelay;
                        prefabs[i].CloseFeedback();
                    }
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

    public void DisableAllFeedbackForChapter(string chapterName)
    {
        if (chapterFilter != null)
            chapterFilter.SetFeedbackLevel(chapterName, 2);

        var feedbacksToClose = activeFeedbackSteps
            .Where(kvp => kvp.Value.chapterName == chapterName)
            .Select(kvp => kvp.Key)
            .ToList();

        if (feedbacksToClose.Count == 0)
            return;

        foreach (var feedback in feedbacksToClose)
        {
            List<FeedbackPrefabController> prefabs = FindFeedbackInstance(feedback.FeedbackName);
            if (prefabs != null)
            {
                for (int i = prefabs.Count - 1; i >= 0; i--)
                    prefabs[i].CloseFeedbackWithoutCompletion();
            }

            activeFeedbackSteps.Remove(feedback);
            shownFeedbacks.Remove(feedback);
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

        return feedbacksToRemove.Count != 0 ? feedbacksToRemove : null;
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
            sender.SetTempoPreStep(tempo);
        }
        else
            Debug.LogWarning($"[FeedbackAutoManager] Nessun sender trovato per '{feedback.FeedbackName}'");
    }

    private void OnChapterMidEvent(string chapterName)
    {
        if (chapterFilter == null || !chapterFilter.IsHardAssistActive(chapterName))
            return;

        Debug.Log($"[FeedbackAutoManager] Hard Assist attivato per '{chapterName}'.");

        ApplyOutlineAll();
        SetWaypointLayer(hardAssistWaypointLayer);
    }

    private void ApplyOutlineAll()
    {
        if (outlineManager == null)
        {
            Debug.LogWarning("[FeedbackAutoManager] OutlineManager non assegnato, impossibile applicare Outline All.");
            return;
        }

        outlineManager.SetOutlineModeAll();
        Debug.Log("[FeedbackAutoManager] OutlineManager: modalità 'Outline All' applicata.");
    }

    private void SetWaypointLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogWarning($"[FeedbackAutoManager] Layer '{layerName}' non trovato nel progetto.");
            return;
        }

        GameObject[] Waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject wp in Waypoints)
            wp.layer = layer;

        GameObject[] sceneWaypoints = GameObject.FindGameObjectsWithTag("WayPointSmall");
        foreach (GameObject wp in sceneWaypoints)
        {
            if (wp.transform.parent == null)
            {
                Debug.LogWarning($"{wp.name} non ha parent");
                continue;
            }

            wp.transform.parent.gameObject.layer = layer;
        }

        Debug.Log($"[FeedbackAutoManager] Layer waypoint impostato a '{layerName}'.");
    }
}