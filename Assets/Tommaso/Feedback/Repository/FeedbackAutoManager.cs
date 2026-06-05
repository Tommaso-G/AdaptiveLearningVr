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

    // Struttura aggiornata: tiene traccia anche del capitolo di appartenenza
    private Dictionary<FeedbackRepository.FeedbackData, (HashSet<string> steps, string chapterName)> activeFeedbackSteps = new();
    private HashSet<FeedbackRepository.FeedbackData> shownFeedbacks = new();

    // Chiave: posizione world arrotondata, Valore: timestamp di fine cooldown
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

    // Arrotonda a 10cm per tollerare piccole variazioni di posizione
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
    string stepName = step.Data.Name;
    string firstStep = feedback.StepForCompletition.FirstOrDefault();

    // LOG A — ingresso
    Debug.Log($"[FAM] ── OnStepActivated | step='{stepName}' | chapter='{chapterName}' | feedback='{feedback.FeedbackName}'");

    // LOG B — filtro capitolo
    if (chapterFilter != null && !chapterFilter.IsFeedbackAllowed(chapterName))
    {
        Debug.Log($"[FAM] EXIT① chapterFilter blocca '{chapterName}'");
        return;
    }

    // LOG C — check primo step
    if (stepName != firstStep)
    {
        Debug.Log($"[FAM] EXIT② step '{stepName}' non è il primo step '{firstStep}', salto.");
        return;
    }

    // LOG D — già mostrato
    if (shownFeedbacks.Contains(feedback))
    {
        Debug.Log($"[FAM] EXIT③ feedback '{feedback.FeedbackName}' già in shownFeedbacks, salto.");
        return;
    }

    // LOG E — ricerca target GameObject
    GameObject target = GetFirstGameObjectFromStep(step);
    if (target == null)
    {
        Debug.LogWarning($"[FAM] EXIT④ GetFirstGameObjectFromStep restituisce null per '{stepName}'.");
        return;
    }
    Debug.Log($"[FAM] target trovato: '{target.name}'");

    // LOG F — ricerca posizioni feedback
    List<Transform> feedbackPositions = feedbackDisplayer.FindFeedbackPositionChild(target);
    if (feedbackPositions == null)
    {
        Debug.LogWarning($"[FAM] EXIT⑤ FindFeedbackPositionChild restituisce null su '{target.name}'.");
        return;
    }
    Debug.Log($"[FAM] posizioni trovate: {feedbackPositions.Count} su '{target.name}'");

    // calcolo delay (invariato) ...
    float delay = 0f;
    foreach (var pos in feedbackPositions)
    {
        Vector3Int key = PositionKey(pos.position);
        if (positionCooldowns.TryGetValue(key, out float until))
        {
            float remaining = until - Time.time;
            if (remaining > delay) delay = remaining;
        }
    }

    // LOG G — avvio coroutine
    Debug.Log($"[FAM] Avvio coroutine | delay={delay:F2}s | feedback='{feedback.FeedbackName}'");

    shownFeedbacks.Add(feedback);
    if (!activeFeedbackSteps.ContainsKey(feedback))
        activeFeedbackSteps[feedback] = (new HashSet<string>(feedback.StepForCompletition), chapterName);

    StartCoroutine(ShowFeedbackAfterDelay(delay, chapterName, feedback, feedbackPositions));
}

private IEnumerator ShowFeedbackAfterDelay(
    float delay,
    string chapterName,
    FeedbackRepository.FeedbackData feedback,
    List<Transform> feedbackPositions)
{
    if (delay > 0f)
        yield return new WaitForSeconds(delay);

    if (!shownFeedbacks.Contains(feedback))
    {
        Debug.LogWarning($"[FAM] Coroutine abortita: '{feedback.FeedbackName}' rimosso da shownFeedbacks durante attesa.");
        yield break;
    }

    feedbackDisplayer.PrepareAndDisplayFeedback(feedback, feedbackPositions, feedbackHolder);

    // LOG H — istanza in scena
    GameObject instance = feedbackHolder.activeFeedbackInstance;
    if (instance != null)
        Debug.Log($"[FAM] ✓ Istanza creata: '{instance.name}'");
    else
        Debug.LogWarning($"[FAM] ✗ PrepareAndDisplayFeedback eseguito ma activeFeedbackInstance è null!");

    // ... resto invariato
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
            var feedback = kvp.Key;
            var remainingSteps = kvp.Value.steps;

            if (remainingSteps.Contains(stepName))
                remainingSteps.Remove(stepName);

            if (remainingSteps.Count == 0)
            {
                List<FeedbackPrefabController> prefabs = FindFeedbackInstance(feedback.FeedbackName);
                if (prefabs != null)
                {
                    for (int i = prefabs.Count - 1; i >= 0; i--)
                    {
                        // Registra cooldown sulla posizione world prima di chiudere
                        Vector3Int key = PositionKey(prefabs[i].transform.position);
                        positionCooldowns[key] = Time.time + feedbackPositionDelay;
                        // Debug.Log($"[FeedbackAutoManager] Cooldown {feedbackPositionDelay}s su posizione {prefabs[i].transform.position}.");

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

    /// <summary>
    /// Disabilita tutti i feedback per il capitolo indicato (anche sottocapitoli)
    /// e chiude quelli già mostrati in scena.
    /// </summary>
    public void DisableAllFeedbackForChapter(string chapterName)
    {
        // 1. Disabilita i feedback futuri tramite chapterFilter
        if (chapterFilter != null)
            chapterFilter.SetFeedbackLevel(chapterName, 2);

        // 2. Trova tutti i feedback attivi che appartengono al capitolo
        var feedbacksToClose = activeFeedbackSteps
            .Where(kvp => kvp.Value.chapterName == chapterName)
            .Select(kvp => kvp.Key)
            .ToList();

        if (feedbacksToClose.Count == 0)
        {
            // Debug.Log($"[FeedbackAutoManager] Nessun feedback attivo trovato per '{chapterName}'.");
            return;
        }

        // 3. Chiudi i prefab in scena e pulisci lo stato interno
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

        //Debug.Log($"[FeedbackAutoManager] Disabilitati e chiusi {feedbacksToClose.Count} feedback per il capitolo '{chapterName}'.");
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
            //Debug.Log($"[RegisterTempoPreStep] Sender trovato per '{feedback.FeedbackName}', tempo: {tempo}");
            sender.SetTempoPreStep(tempo);
        }
        else
            Debug.LogWarning($"[FeedbackAutoManager] Nessun sender trovato per '{feedback.FeedbackName}'");
    }

    // ─────────────────────────────────────────────────────────────────
    // HARD ASSIST —
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chiamato da ChapterTimer.OnMidEventTriggered quando il mid-event abbassa
    /// il feedbackLevel a -1. Attiva outline "Outline All" e sposta i waypoint
    /// sul layer Default.
    /// </summary>
    private void OnChapterMidEvent(string chapterName)
    {
        if (chapterFilter == null || !chapterFilter.IsHardAssistActive(chapterName))
            return;

        Debug.Log($"[FeedbackAutoManager] Hard Assist attivato per '{chapterName}'.");

        ApplyOutlineAll();
        SetWaypointLayer(hardAssistWaypointLayer);
    }

    /// <summary>
    /// Imposta OutlineMode = OutlineAll su tutti gli oggetti gestiti dall'OutlineManager.
    /// </summary>
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

    /// <summary>
    /// Cambia il layer di tutti i waypoint istanziati da FeedbackPrefabController
    /// e di tutti i GameObject taggati WayPointSmall nella scena.
    /// </summary>
    private void SetWaypointLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogWarning($"[FeedbackAutoManager] Layer '{layerName}' non trovato nel progetto.");
            return;
        }

        // Waypoint nei FeedbackPrefabController attivi
        GameObject[] Waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject wp in Waypoints)
            wp.layer = layer;

        // Waypoint liberi nella scena (tag WayPointSmall)
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