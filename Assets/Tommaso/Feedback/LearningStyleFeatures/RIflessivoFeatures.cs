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
    private readonly List<ClosableDoor> closableDoors= new();
    private readonly List<Animator> pausedAnimators= new();
    private readonly List<ParticleSystem> pausedParticles = new();
    private readonly List<AgentState> pausedNavMeshAgents = new();
    public static bool IsPaused { get; private set; } = false;




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
        //Debug.Log("[RiflessivoFeatures] Feature disabilitata manualmente. Reset immediato.");
        
        // Se disabilitiamo la feature mentre un feedback è attivo, ripristiniamo tutto subito
        ResetReflectiveEffects(Object.FindFirstObjectByType<FeedbackPrefabController>());
    }


    // --- CICLO DI VITA FEEDBACK ---

    public override void OnFeedbackOpened(FeedbackPrefabController feedback)
    {
        if (feedback == null || !isTimeStopFeatureEnabled)
        {
            //Debug.Log($"impossibile, isTimeStopFeature è: {isTimeStopFeatureEnabled}");
            return;
        }
        

        //Debug.Log($"[RiflessivoFeatures] Apertura feedback: applicazione effetti.");
        ApplyReflectiveEffects(feedback);
    }

    public override void OnFeedbackClosed(FeedbackPrefabController feedback)
    {
        if (feedback == null) return;

        //Debug.Log($"[RiflessivoFeatures] Chiusura feedback: ripristino ambiente.");
        ResetReflectiveEffects(feedback);
    }

    public override void resetVariables()
    {
        isTimeStopFeatureEnabled = true;
    }

    // --- LOGICA ESECUZIONE EFFETTI ---

    private void ApplyReflectiveEffects(FeedbackPrefabController feedback)
    {
        EnsureVolumeReference();

        // 1. Post Process
        if (globalVolume != null)
            feedback.StartCoroutine(FadeVolumeWeight(globalVolume, 1f, 0.5f));

        // 2. Audio
        feedback.StartCoroutine(FadeAudioVolume(AudioListener.volume, 0f, audioFadeDuration));

        // 3. Blocca Interazioni
        DisableInteractablesInRange(feedback.transform.position);

        PauseAnimators();

        PauseParticles();

        PauseNavMeshAgent();

        SetPaused(true);

    }

    private void ResetReflectiveEffects(FeedbackPrefabController feedback)
    {
        EnsureVolumeReference();

        // 1. Post Process
        if (globalVolume != null && feedback != null)
            feedback.StartCoroutine(FadeVolumeWeight(globalVolume, 0f, 0.5f));
        else if (globalVolume != null)
            globalVolume.weight = 0;

        // 2. Audio
        if (feedback != null)
            feedback.StartCoroutine(FadeAudioVolume(AudioListener.volume, 1f, audioFadeDuration));
        else
            AudioListener.volume = 1;

        // 3. Riabilita Interazioni
        EnableInteractables();

        ResumeAnimators();

        ResumeParticles();

        ResumeNavMeshAgents();

        SetPaused(false);

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

        Collider[] nearbyObjects = Physics.OverlapSphere(center, interactionRadius);
        //Debug.Log($"[RiflessivoFeatures] Scansione oggetti nel raggio di {interactionRadius} attorno a {center}. Trovati {nearbyObjects.Length} collider.");

        foreach (Collider col in nearbyObjects)
        {
            // 1. XRBaseInteractable
            var interactables = col.GetComponentsInChildren<XRBaseInteractable>(true);
            if (interactables.Length > 0)
            {
                //Debug.Log($"[RiflessivoFeatures] {col.gameObject.name} contiene {interactables.Length} interattabili:");

                foreach (var interactable in interactables)
                {
                    if (interactable == null)
                    {
                        Debug.LogWarning($"[RiflessivoFeatures] Componente nullo su {col.gameObject.name} — saltato.");
                        continue;
                    }

                    //Debug.Log($"    - {interactable.GetType().Name} (enabled={interactable.enabled})");

                    if (interactable.enabled)
                    {
                        interactable.enabled = false;
                        disabledInteractables.Add(interactable);
                        //Debug.Log($"      → DISATTIVATO: {interactable.name}");
                    }
                    else
                    {
                        //Debug.Log($"      → Già disattivato: {interactable.name}");
                    }
                }
            }

            // 2. ClosableDoor
            var doors = col.GetComponentsInChildren<ClosableDoor>(true);
            foreach (var door in doors)
            {
                if (door == null)
                    continue;

                if (door.enabled)
                {
                    door.enabled = false;
                    closableDoors.Add(door);
                    //Debug.Log($"[RiflessivoFeatures] → DISATTIVATO Door: {door.name}");
                }
                else
                {
                    //Debug.Log($"[RiflessivoFeatures] → Porta già disattivata: {door.name}");
                }
            }
        }

        //Debug.Log($"[RiflessivoFeatures] Totale interattabili disattivati: {disabledInteractables.Count}, porte disattivate: {closableDoors.Count}");
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

        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        
        foreach (Animator animator in animators)
        {
            if(animator.enabled && animator.gameObject.activeInHierarchy)
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

        ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

        foreach (ParticleSystem ps in particleSystems)
        {
            if(ps.isPlaying && ps.gameObject.activeInHierarchy)
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
        var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

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