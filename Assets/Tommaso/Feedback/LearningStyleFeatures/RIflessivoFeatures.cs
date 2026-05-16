using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core;

[CreateAssetMenu(menuName = "Learning Styles/Riflessivo Behaviour")]
public class RiflessivoFeatures : LearningStyleFeatures
{
    [Header("Settings")]
    public float audioFadeDuration = 1.5f;
    [SerializeField] private float interactionRadius = 5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("State")]
    [SerializeField] private bool isTimeStopFeatureEnabled = true;

    private Volume globalVolume;
    private readonly List<XRBaseInteractable> disabledInteractables = new();
    private readonly List<XRPushButtonVrBuilder> pausedPushButtons = new();
    private readonly List<ClosableDoor> closableDoors = new();
    private readonly List<Animator> pausedAnimators = new();
    private readonly List<ParticleSystem> pausedParticles = new();
    private readonly List<AgentState> pausedNavMeshAgents = new();
    public static bool IsPaused { get; private set; } = false;

    private FeedbackAutoManager _safeRunner;

    private FeedbackAutoManager SafeRunner
    {
        get
        {
            if (_safeRunner == null)
                _safeRunner = Object.FindFirstObjectByType<FeedbackAutoManager>();
            return _safeRunner;
        }
    }


    // --- LOGICA DI CONTROLLO STATO ---

    public override void EnableFeature()
    {
        isTimeStopFeatureEnabled = true;
        Debug.Log("[RiflessivoFeatures] Feature abilitata manualmente.");
        ApplyReflectiveEffects(Object.FindFirstObjectByType<FeedbackPrefabController>());
    }

    public override void DisableFeature()
    {
        isTimeStopFeatureEnabled = false;
        ResetReflectiveEffects(Object.FindFirstObjectByType<FeedbackPrefabController>());
    }


    // --- CICLO DI VITA FEEDBACK ---

    public override void OnFeedbackOpened(FeedbackPrefabController feedback)
    {
        if (feedback == null || !isTimeStopFeatureEnabled)
        {
            Debug.Log($"impossibile, isTimeStopFeature è: {isTimeStopFeatureEnabled}");
            return;
        }

        Debug.Log($"[RiflessivoFeatures] Apertura feedback: applicazione effetti.");
        ApplyReflectiveEffects(feedback);
    }

    public override void OnFeedbackClosed(FeedbackPrefabController feedback)
    {
        if (feedback == null) return;
        SafeRunner?.RunCoroutineSafe(WaitForVolumeAndReset(feedback));
    }

    public override void resetVariables()
    {
        isTimeStopFeatureEnabled = true;
    }


    // --- LOGICA ESECUZIONE EFFETTI ---

    private void ApplyReflectiveEffects(FeedbackPrefabController feedback)
    {
        EnsureVolumeReference();

        if (globalVolume != null)
            SafeRunner?.RunCoroutineSafe(FadeVolumeWeight(globalVolume, 1f, 0.5f));

        SafeRunner?.RunCoroutineSafe(FadeAudioVolume(AudioListener.volume, 0f, audioFadeDuration));

        DisableInteractablesInRange(feedback.transform.position);

        PauseAnimators();
        PauseParticles();
        PauseNavMeshAgent();

        SetPaused(true);
    }

    private void ResetReflectiveEffects(FeedbackPrefabController feedback)
    {
        // Volume già gestito da WaitForVolumeAndReset

        SafeRunner?.RunCoroutineSafe(FadeAudioVolume(AudioListener.volume, 1f, audioFadeDuration));

        EnableInteractables();
        ResumeAnimators();
        ResumeParticles();
        ResumeNavMeshAgents();

        SetPaused(false);
    }

    private IEnumerator WaitForVolumeAndReset(FeedbackPrefabController feedback)
    {
        EnsureVolumeReference();

        if (globalVolume != null)
        {
            float elapsed = 0f;
            float startWeight = globalVolume.weight;
            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                globalVolume.weight = Mathf.Lerp(startWeight, 0f, elapsed / audioFadeDuration);
                yield return null;
            }
            globalVolume.weight = 0f;
        }

        ResetReflectiveEffects(feedback);
    }

    private void EnsureVolumeReference()
    {
        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();
    }


    private void DisableInteractablesInRange(Vector3 center)
    {

        disabledInteractables.Clear();
        closableDoors.Clear();

        Collider[] nearbyObjects = Physics.OverlapSphere(center, interactionRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider col in nearbyObjects)
        {
                Debug.Log($"[RiflessivoFeatures] Collider trovato: {col.gameObject.name}");

            var interactables = col.GetComponentsInChildren<XRBaseInteractable>(true);
            if (interactables.Length > 0)
            {
                foreach (var interactable in interactables)
                {
                    if (interactable == null)
                    {
                        Debug.LogWarning($"[RiflessivoFeatures] Componente nullo su {col.gameObject.name} — saltato.");
                        continue;
                    }

                    if (interactable.CompareTag("Feedback"))
                        continue;

                    if (interactable.enabled)
                    {
                        interactable.enabled = false;
                        disabledInteractables.Add(interactable);
                    }
                }
            }

            var doors = col.GetComponentsInChildren<ClosableDoor>(true);
            foreach (var door in doors)
            {
                if (door == null) continue;

                if (door.enabled)
                {
                    door.enabled = false;
                    closableDoors.Add(door);
                }
            }

            var pushButtons = col.GetComponentsInChildren<XRPushButtonVrBuilder>(true);
            foreach (var btn in pushButtons)
            {
                if (btn == null) continue;
                Debug.Log($"[RiflessivoFeatures] PushButton trovato: {btn.name}, enabled: {btn.enabled}");
                
                if (btn.enabled)
                {
                    btn.enabled = false;
                    // Disabilita anche il collider
                    var btnCollider = btn.GetComponent<Collider>();
                    if (btnCollider != null)
                    {
                        btnCollider.enabled = false;
                        Debug.Log($"[RiflessivoFeatures] Collider disabilitato su: {btn.name}");
                    }
                    pausedPushButtons.Add(btn);
                }
            }
        }
    }

    private void EnableInteractables()
    {
        foreach (var interactable in disabledInteractables)
        {
            if (interactable != null)
                interactable.enabled = true;
        }

        foreach (var door in closableDoors)
        {
            if (door != null)
                door.enabled = true;
        }

        disabledInteractables.Clear();
        closableDoors.Clear();

        Debug.Log("[RiflessivoFeatures] Tutti gli interattabili e le porte sono stati riattivati.");

        foreach (var btn in pausedPushButtons)
        {
            if (btn != null)
                btn.enabled = true;
        }
        pausedPushButtons.Clear();
    }


    private IEnumerator FadeVolumeWeight(Volume volume, float targetWeight, float duration)
    {
        float startWeight = volume.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            volume.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            yield return null;
        }
        volume.weight = targetWeight;
    }

    private IEnumerator FadeAudioVolume(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        AudioListener.volume = end;
    }

    public void PauseAnimators()
    {
        pausedAnimators.Clear();

        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);

        foreach (Animator animator in animators)
        {
            if (animator.enabled && animator.gameObject.activeInHierarchy)
            {
                animator.enabled = false;
                pausedAnimators.Add(animator);
            }
        }
    }

    private void ResumeAnimators()
    {
        foreach (Animator animator in pausedAnimators)
        {
            if (animator != null)
                animator.enabled = true;
        }
        pausedAnimators.Clear();
    }

    private void PauseParticles()
    {
        pausedParticles.Clear();

        ParticleSystem[] particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps.isPlaying && ps.gameObject.activeInHierarchy)
            {
                ps.Pause();
                pausedParticles.Add(ps);
            }
        }
    }

    private void ResumeParticles()
    {
        foreach (ParticleSystem ps in pausedParticles)
        {
            if (ps != null)
                ps.Play();
        }
        pausedParticles.Clear();
    }

    private class AgentState
    {
        public NavMeshAgent agent;
        public Vector3 destination;
    }

    private void PauseNavMeshAgent()
    {
        pausedNavMeshAgents.Clear();
        var agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

        foreach (var nma in agents)
        {
            if (nma != null && nma.gameObject.activeInHierarchy && !nma.isStopped)
            {
                pausedNavMeshAgents.Add(new AgentState
                {
                    agent = nma,
                    destination = nma.hasPath ? nma.destination : nma.transform.position
                });

                nma.isStopped = true;
            }
        }
    }

    private void ResumeNavMeshAgents()
    {
        foreach (var state in pausedNavMeshAgents)
        {
            if (state.agent != null)
            {
                state.agent.isStopped = false;
                state.agent.SetDestination(state.destination);
            }
        }

        pausedNavMeshAgents.Clear();
    }

    public static void SetPaused(bool value)
    {
        IsPaused = value;
    }

    public override void OnStepActivated(IStep step) { }
    public override void OnStepCompleted(IStep step) { }
}