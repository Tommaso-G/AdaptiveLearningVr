using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;

public class StepAudioManager : MonoBehaviour
{
    private const string OBJECT_STEP_TAG = "ObjectStep";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip stepAudioClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Audio Source (2D MASTER)")]
    [SerializeField] private AudioSource audioSource;

    private IProcess process;

    // Stato step precedente
    private HashSet<GameObject> lastStepObjects = new HashSet<GameObject>();

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 0f; // 🔥 2D
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.StepStarted += OnStepStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.StepStarted -= OnStepStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
        lastStepObjects.Clear();
    }

    private void OnStepStarted(object sender, ProcessEventArgs args)
    {
        if (process == null) return;

        IChapter chapter = process.Data.Current;
        if (chapter == null) return;

        IStep step = chapter.Data.Current;
        if (step == null) return;

        CollectTargetObjects(step, out List<GameObject> currentObjects);

        HashSet<GameObject> currentSet = currentObjects
            .Where(o => o != null && o.CompareTag(OBJECT_STEP_TAG))
            .ToHashSet();

        // 🔴 BLOCCO: se non ci sono oggetti validi, NON fare nulla
        if (currentSet.Count == 0)
        {
            Debug.Log("[StepAudioManager] Nessun ObjectStep valido → audio non attivato");
            return;
        }

        bool changed =
            currentSet.Count != lastStepObjects.Count ||
            !currentSet.SetEquals(lastStepObjects);

        if (changed)
        {
            PlaySound();

            string names = string.Join(", ", currentSet.Select(o => o.name));
            Debug.Log($"[StepAudioManager] Audio triggerato da: {names}");
        }

        lastStepObjects = new HashSet<GameObject>(currentSet);
    }

    private void PlaySound()
    {
        if (stepAudioClip == null) return;

        audioSource.PlayOneShot(stepAudioClip, volume);

        Debug.Log("[StepAudioManager] Audio triggerato per cambio ObjectStep");
    }

    // -------------------------------------------------------------------------
    // COLLECTION (come tuo sistema VRBuilder)
    // -------------------------------------------------------------------------
    private void CollectTargetObjects(IStep step, out List<GameObject> result)
    {
        result = new List<GameObject>();

        foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
        {
            if (transition is not Transition t) continue;

            foreach (ICondition condition in t.Data.Conditions)
            {
                var props = condition.Data.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                {
                    if (typeof(SingleSceneObjectReference)
                        .IsAssignableFrom(prop.PropertyType))
                    {
                        CollectFromSSO(prop, condition, result);
                    }
                }
            }
        }
    }

    private void CollectFromSSO(PropertyInfo prop, ICondition condition, List<GameObject> result)
    {
        var ssor = prop.GetValue(condition.Data) as SingleSceneObjectReference;
        if (ssor == null) return;

        foreach (var guid in ssor.Guids)
        {
            foreach (var obj in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetObjects(guid))
            {
                if (obj is ProcessSceneObject pso && !result.Contains(pso.GameObject))
                {
                    result.Add(pso.GameObject);
                }
            }
        }
    }
}