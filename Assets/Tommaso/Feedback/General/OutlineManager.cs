using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using Unity.VisualScripting;
using UnityEngine.UI;

/// <summary>
/// Per ogni step, cerca gli oggetti con tag "ObjectStep" (o nei loro figli)
/// che abbiano un componente Outline, e lo attiva/disattiva insieme allo step.
/// Lo spessore (OutlineWidth) oscilla ciclicamente tra MinOutlineWidth e MaxOutlineWidth
/// con un periodo configurabile dall'inspector.
/// </summary>
public class StepOutlineManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Costanti
    // -------------------------------------------------------------------------
    private const string OBJECT_STEP_TAG = "ObjectStep";

    // -------------------------------------------------------------------------
    // Campi configurabili dall'inspector
    // -------------------------------------------------------------------------
    [Header("Outline Pulse Settings")]

    [Tooltip("Valore minimo dell'OutlineWidth durante l'oscillazione.")]
    [Range(0f, 20f)]
    [SerializeField] private float minOutlineWidth = 2f;

    [Tooltip("Valore massimo dell'OutlineWidth durante l'oscillazione.")]
    [Range(0f, 20f)]
    [SerializeField] private float maxOutlineWidth = 8f;

    [Tooltip("Durata in secondi di mezzo ciclo (da min a max, o da max a min).")]
    [Min(0.01f)]
    [SerializeField] private float halfCycleDuration = 0.5f;

    [Tooltip("Tipo di interpolazione per l'oscillazione dello spessore.")]
    [SerializeField] private PulseEasing easingType = PulseEasing.SineInOut;

    [Header("Filtro capitoli")]
    public FeedbackChapterFilter chapterFilter;

    // -------------------------------------------------------------------------
    // Enumerazione del tipo di easing
    // -------------------------------------------------------------------------
    public enum PulseEasing
    {
        Linear,
        SineInOut
    }

    // -------------------------------------------------------------------------
    // Stato interno
    // -------------------------------------------------------------------------
    private IProcess process;

    /// <summary>Outline attivi nello step corrente, da spegnere alla fine.</summary>
    private readonly List<Outline> activeOutlines = new List<Outline>();
    private readonly Dictionary<VisualProxy, Outline> activeProxyOutlines = new Dictionary<VisualProxy, Outline>();

    private Coroutine pulseCoroutine;

    // Stato interno aggiuntivo in StepOutlineManager
    private readonly Dictionary<IChapter, IStep> trackedSubChapterSteps = new Dictionary<IChapter, IStep>();
    private Coroutine subChapterMonitorCoroutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Start()
    {
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.StepStarted += OnStepStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.StepStarted -= OnStepStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
        StopSubChapterMonitor();
        if (activeProxyOutlines.Count > 0)
        {
            foreach (KeyValuePair<VisualProxy, Outline> pair in activeProxyOutlines)
            {
                pair.Key.OnProxyChanged -= HandleProxyOutline;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------
    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
    }

    private void OnStepStarted(object sender, ProcessEventArgs args)
    {
        if (process == null) return;

        IChapter chapter = process.Data.Current;
        if (chapter == null) return;


        DisableAllActiveOutlines();
        DisableAllActiveProxyOutlines();

        // Controlla il filtro: outline attivo solo se feedbackLevel è 0 o 1
        if (chapterFilter != null && chapterFilter.IsOutlineAllowed(chapter.Data.Name) == false)
            return;

        if (TryEnableOutlinesInSubChapters(chapter)) return;

        IStep step = chapter.Data.Current;
        if (step == null) return;

        EnableOutlinesForStep(step);
    }


    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        DisableAllActiveOutlines();
        DisableAllActiveProxyOutlines();
    }

    // -------------------------------------------------------------------------
    // Ricerca dello step attivo (con supporto sotto-capitoli)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Se lo step corrente del capitolo ha un ExecuteChaptersBehavior,
    /// cerca lo step attivo in ciascun sotto-capitolo e ne attiva gli outline.
    /// Restituisce true se ha gestito lui la situazione, false se il capitolo è normale.
    /// </summary>
    private bool TryEnableOutlinesInSubChapters(IChapter chapter)
    {
        foreach (IStep step in chapter.Data.Steps)
        {
            IBehavior behavior = step.Data.Behaviors?.Data.Behaviors.FirstOrDefault();
            if (behavior == null) continue;

            if (behavior is ExecuteChaptersBehavior executeChaptersBehavior)
            {
                StopSubChapterMonitor();
                // Passa il behavior, non i sottocapitoli già risolti
                subChapterMonitorCoroutine = StartCoroutine(
                    MonitorSubChapters(executeChaptersBehavior));
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Monitora ogni frame i sottocapitoli: quando uno step cambia,
    /// aggiorna gli outline esattamente come farebbe StepStarted.
    /// </summary>
    private IEnumerator MonitorSubChapters(ExecuteChaptersBehavior executeChaptersBehavior)
    {
        // Aspetta finché almeno un sottocapitolo ha uno step attivo
        List<IChapter> subChapters = null;

        while (true)
        {
            subChapters = executeChaptersBehavior.Data.GetChildren()
                .Where(c => c != null)
                .ToList();

            bool anyActive = subChapters.Any(c => c?.Data?.Current != null);
            if (anyActive) break;

            yield return null;
        }

        Debug.Log($"[StepOutlineManager] Sottocapitoli attivi trovati: {subChapters.Count}");

        // Abilita gli outline per lo step attivo iniziale
        foreach (IChapter subChapter in subChapters)
        {
            IStep activeStep = subChapter?.Data?.Current;
            if (activeStep != null)
                EnableOutlinesForStep(activeStep);
        }

        // Snapshot iniziale
        var lastStep = new Dictionary<IChapter, IStep>();
        foreach (IChapter subChapter in subChapters)
            lastStep[subChapter] = subChapter?.Data?.Current;

        // Monitor continuo
        while (true)
        {
            bool anyChanged = false;

            foreach (IChapter subChapter in subChapters)
            {
                if (subChapter?.Data == null) continue;

                IStep currentStep = subChapter.Data.Current;

                if (currentStep != lastStep[subChapter])
                {
                    Debug.Log($"[StepOutlineManager] Last step: {lastStep[subChapter].Data.Name}. Current step: {(currentStep == null ? "Finito il sottocapitolo" : currentStep.Data.Name)}");
                    lastStep[subChapter] = currentStep;
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                ClearOutlines();
                ClearProxyOutlines();

                foreach (IChapter subChapter in subChapters)
                {
                    if (subChapter?.Data == null) continue;

                    IStep activeStep = subChapter.Data.Current;
                    if (activeStep != null)
                    {
                        Debug.Log($"[StepOutlineManager] Step cambiato: {activeStep.Data.Name} in {subChapter.Data.Name}");
                        EnableOutlinesForStep(activeStep);
                    }
                }
            }

            yield return null;
        }
    }

    private void StopSubChapterMonitor()
    {
        if (subChapterMonitorCoroutine != null)
        {
            StopCoroutine(subChapterMonitorCoroutine);
            subChapterMonitorCoroutine = null;
        }
        trackedSubChapterSteps.Clear();
    }
    // -------------------------------------------------------------------------
    // Logica principale
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raccoglie tutti i GameObjects target dello step (tramite condizioni)
    /// e per ognuno cerca un Outline sul GameObject o sui suoi figli con tag "ObjectStep".
    /// </summary>
    private void EnableOutlinesForStep(IStep step)
    {
        CollectTargetObjects(step,
            out List<GameObject> standardObjects,
            out List<GameObject> proxyObjects);

        foreach (GameObject go in standardObjects)
        {
            FindAndEnableOutlines(go);
        }

        foreach (GameObject go in proxyObjects)
        {
            VisualProxy vproxy = go.GetComponent<VisualProxy>();
            GameObject activeProxy = vproxy?.activeproxy;
            if (activeProxy != null)
            {
                Debug.Log($"[StepOutlineManager] ActiveProxy {activeProxy.name}, Visual Proxy {vproxy.name}");
                FindAndEnableProxyOutlines(activeProxy, vproxy);
                vproxy.OnProxyChanged += HandleProxyOutline;
            }
            else
            {
                Debug.Log($"[StepOutlineManager] ActiveProxy null");
            }
        }

        // Avvia la coroutine di pulsazione se ci sono outline attivi
        if (activeOutlines.Count > 0 || activeProxyOutlines.Count > 0)
        {
            StopPulseCoroutine();
            pulseCoroutine = StartCoroutine(PulseOutlineWidth());
        }
    }

    private void HandleProxyOutline(VisualProxy vproxy)
    {
        StartCoroutine(UpdateProxyOutline(vproxy));
    }

    private IEnumerator UpdateProxyOutline(VisualProxy vproxy)
    {
        if (activeProxyOutlines.ContainsKey(vproxy))
        {
            activeProxyOutlines[vproxy].OutlineWidth = minOutlineWidth;
            activeProxyOutlines[vproxy].enabled = false;

            Debug.Log($"[StepOutlineManager] Proxy outline disattivato su: {activeProxyOutlines[vproxy].gameObject.name}");

            activeProxyOutlines.Remove(vproxy);
        }
        else
        {
            GameObject activeProxy = vproxy?.activeproxy;
            if (activeProxy != null)
            {
                FindAndEnableProxyOutlines(activeProxy, vproxy);
            }
        }
            yield return null;
    }
    /// <summary>
    /// Cerca ricorsivamente tra il GO e i suoi figli quelli con tag "ObjectStep"
    /// che hanno un componente Outline, e lo attiva.
    /// </summary>
    private void FindAndEnableOutlines(GameObject root)
    {
        // Il root deve avere il tag ObjectStep
        if (!root.CompareTag(OBJECT_STEP_TAG)) return;

        // Attiva outline su root e su tutti i figli che hanno il componente, indipendentemente dal tag
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            Outline outline = t.GetComponent<Outline>();
            if (outline == null) continue;
            if (activeOutlines.Contains(outline)) continue;

            outline.enabled = true;
            outline.OutlineWidth = minOutlineWidth;
            activeOutlines.Add(outline);

            Debug.Log($"[StepOutlineManager] Outline attivato su: {t.gameObject.name}");
        }
    }

    private void FindAndEnableProxyOutlines(GameObject root, VisualProxy vproxy)
    {
        // Il root deve avere il tag ObjectStep
        if (!root.CompareTag(OBJECT_STEP_TAG))
        {
            Debug.Log($"[StepOutlineManager] Root tag sbagliato. Root = {root.name}");
            return;
        }

        // Attiva outline su root e su tutti i figli che hanno il componente, indipendentemente dal tag
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            Outline outline = t.GetComponent<Outline>();
            if (outline == null) continue;
            if (activeProxyOutlines.ContainsKey(vproxy)) continue;

            outline.enabled = true;
            outline.OutlineWidth = minOutlineWidth;
            activeProxyOutlines.Add(vproxy, outline);

            Debug.Log($"[StepOutlineManager] Proxy outline attivato su: {t.gameObject.name}");
        }
    }

    private void TryEnableOutline(GameObject go)
    {
        if (!go.CompareTag(OBJECT_STEP_TAG)) return;

        Outline outline = go.GetComponent<Outline>();
        if (outline == null) return;

        if (activeOutlines.Contains(outline)) return;

        outline.enabled = true;
        outline.OutlineWidth = minOutlineWidth;
        activeOutlines.Add(outline);

        Debug.Log($"[StepOutlineManager] Outline attivato su: {go.name}");
    }

    private void DisableAllActiveOutlines()
    {
        StopSubChapterMonitor();
        StopPulseCoroutine();
        ClearOutlines();
    }

    private void ClearOutlines()
    {
        StopPulseCoroutine();

        foreach (Outline outline in activeOutlines)
        {
            if (outline != null)
            {
                outline.OutlineWidth = minOutlineWidth;
                outline.enabled = false;
                Debug.Log($"[StepOutlineManager] Outline disattivato su: {outline.gameObject.name}");
            }
        }
        activeOutlines.Clear();
    }

    private void DisableAllActiveProxyOutlines()
    {
        StopSubChapterMonitor();
        StopPulseCoroutine();
        ClearProxyOutlines();
    }

    private void ClearProxyOutlines()
    {
        StopPulseCoroutine();

        foreach (KeyValuePair<VisualProxy, Outline> pair in activeProxyOutlines)
        {
            pair.Value.OutlineWidth = minOutlineWidth;
            pair.Value.enabled = false;
            pair.Key.OnProxyChanged -= HandleProxyOutline;
            Debug.Log($"[StepOutlineManager] Outline disattivato su: {pair.Value.gameObject.name}");
        }
        activeProxyOutlines.Clear();
    }

    private void StopPulseCoroutine()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    // -------------------------------------------------------------------------
    // Coroutine di oscillazione
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fa oscillare l'OutlineWidth di tutti gli outline attivi
    /// tra minOutlineWidth e maxOutlineWidth in modo continuo e ciclico.
    /// </summary>
    private IEnumerator PulseOutlineWidth()
    {
        // Garantiamo che min < max per evitare comportamenti strani
        float lo = Mathf.Min(minOutlineWidth, maxOutlineWidth);
        float hi = Mathf.Max(minOutlineWidth, maxOutlineWidth);

        float t = 0f;
        bool goingUp = true;

        while (true)
        {
            t += Time.deltaTime / halfCycleDuration;

            if (t >= 1f)
            {
                t = t - 1f; // mantiene il resto per continuità
                goingUp = !goingUp;
            }

            float normalized = goingUp ? t : 1f - t;
            float eased = ApplyEasing(normalized);
            float width = Mathf.LerpUnclamped(lo, hi, eased);

            foreach (Outline outline in activeOutlines)
            {
                if (outline != null)
                    outline.OutlineWidth = width;
            }

            foreach (KeyValuePair<VisualProxy, Outline> pair in activeProxyOutlines)
            {
                pair.Value.OutlineWidth = width;
            }

            yield return null;
        }
    }

    /// <summary>Applica il tipo di easing selezionato a un valore normalizzato [0, 1].</summary>
    private float ApplyEasing(float t)
    {
        switch (easingType)
        {
            case PulseEasing.SineInOut:
                return (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;
            default: // Linear
                return t;
        }
    }

    // -------------------------------------------------------------------------
    // Raccolta oggetti target
    // -------------------------------------------------------------------------

    /// <summary>
    /// Legge le condizioni dello step e raccoglie i GameObjects referenziati,
    /// tramite SingleSceneObjectReference e MultipleScenePropertyReference.
    /// </summary>
    private void CollectTargetObjects(IStep step,
    out List<GameObject> standardObjects,
    out List<GameObject> proxyObjects)
    {
        standardObjects = new List<GameObject>();
        proxyObjects = new List<GameObject>();

        foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
        {
            Transition t = transition as Transition;
            if (t == null) continue;

            foreach (ICondition condition in t.Data.Conditions)
            {
                bool isProxyCondition = condition is VirtualGrabCondition
                                     || condition is ObjectDisabledCondition;

                List<GameObject> target = isProxyCondition ? proxyObjects : standardObjects;

                var properties = condition.Data.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo prop in properties)
                {
                    if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
                        CollectFromSSO(prop, condition, target);
                    else if (prop.PropertyType.IsGenericType &&
                             prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
                        CollectFromMSPR(prop, condition, target);
                }
            }
        }
    }

    private void CollectFromSSO(PropertyInfo prop, ICondition condition, List<GameObject> result)
    {
        SingleSceneObjectReference ssor = prop.GetValue(condition.Data) as SingleSceneObjectReference;
        if (ssor == null) return;

        foreach (Guid guid in ssor.Guids)
        {
            foreach (ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetObjects(guid))
            {
                ProcessSceneObject pso = sceneObject as ProcessSceneObject;
                if (pso != null && !result.Contains(pso.GameObject))
                    result.Add(pso.GameObject);
            }
        }
    }

    private void CollectFromMSPR(PropertyInfo prop, ICondition condition, List<GameObject> result)
    {
        object multiReference = prop.GetValue(condition.Data);
        if (multiReference == null) return;

        PropertyInfo guidsProperty = multiReference.GetType().GetProperty("Guids");
        IEnumerable<Guid> guids = guidsProperty?.GetValue(multiReference) as IEnumerable<Guid>;
        if (guids == null) return;

        foreach (Guid guid in guids)
        {
            foreach (ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetObjects(guid))
            {
                ProcessSceneObject pso = sceneObject as ProcessSceneObject;
                if (pso != null && !result.Contains(pso.GameObject))
                    result.Add(pso.GameObject);
            }
        }
    }
}