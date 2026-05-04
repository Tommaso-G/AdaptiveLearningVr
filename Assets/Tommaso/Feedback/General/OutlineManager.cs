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

    private Coroutine pulseCoroutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Start()
    {
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.StepStarted    += OnStepStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.StepStarted    -= OnStepStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
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
                IEnumerable<IChapter> subChapters = executeChaptersBehavior.Data.GetChildren();
                foreach (IChapter subChapter in subChapters)
                {
                    IStep activeStep = subChapter.Data.Current;
                    if (activeStep != null)
                        EnableOutlinesForStep(activeStep);
                }
                return true;
            }
        }
        return false;
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
        List<GameObject> targetObjects = CollectTargetObjects(step);

        foreach (GameObject go in targetObjects)
        {
            FindAndEnableOutlines(go);
        }

        // Avvia la coroutine di pulsazione se ci sono outline attivi
        if (activeOutlines.Count > 0)
        {
            StopPulseCoroutine();
            pulseCoroutine = StartCoroutine(PulseOutlineWidth());
        }
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

        float t        = 0f;
        bool  goingUp  = true;

        while (true)
        {
            t += Time.deltaTime / halfCycleDuration;

            if (t >= 1f)
            {
                t       = t - 1f; // mantiene il resto per continuità
                goingUp = !goingUp;
            }

            float normalized = goingUp ? t : 1f - t;
            float eased      = ApplyEasing(normalized);
            float width      = Mathf.LerpUnclamped(lo, hi, eased);

            foreach (Outline outline in activeOutlines)
            {
                if (outline != null)
                    outline.OutlineWidth = width;
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
    private List<GameObject> CollectTargetObjects(IStep step)
    {
        var result = new List<GameObject>();

        foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
        {
            Transition t = transition as Transition;
            if (t == null) continue;

            foreach (ICondition condition in t.Data.Conditions)
            {
                var properties = condition.Data.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo prop in properties)
                {
                    if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
                    {
                        CollectFromSSO(prop, condition, result);
                    }
                    else if (prop.PropertyType.IsGenericType &&
                             prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
                    {
                        CollectFromMSPR(prop, condition, result);
                    }
                }
            }
        }

        return result;
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