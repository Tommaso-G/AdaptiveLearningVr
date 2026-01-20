using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        Debug.Log("[RiflessivoFeatures] Feature disabilitata manualmente. Reset immediato.");
        
        // Se disabilitiamo la feature mentre un feedback è attivo, ripristiniamo tutto subito
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

        Debug.Log($"[RiflessivoFeatures] Chiusura feedback: ripristino ambiente.");
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

        // 4. Tempo
        Time.timeScale = 0;
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

        // 4. Riprendi tempo
        Time.timeScale = 1;
    }

    // --- UTILITY ---

    private void EnsureVolumeReference()
    {
        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();
    }

    private void DisableInteractablesInRange(Vector3 center)
    {
        disabledInteractables.Clear();
        Collider[] nearbyObjects = Physics.OverlapSphere(center, interactionRadius, interactableLayer);

        Debug.Log($"[RiflessivoFeatures] Scansione oggetti nel raggio di {interactionRadius} attorno a {center}. Trovati {nearbyObjects.Length} collider.");

        foreach (Collider col in nearbyObjects)
        {
            // Trova tutti i componenti che derivano da XRBaseInteractable, anche nei figli
            var interactables = col.GetComponentsInChildren<XRBaseInteractable>(true);

            if (interactables.Length == 0)
            {
                Debug.Log($"[RiflessivoFeatures] Nessun XRBaseInteractable trovato in {col.gameObject.name}.");
                continue;
            }

            Debug.Log($"[RiflessivoFeatures] {col.gameObject.name} contiene {interactables.Length} interattabili:");

            foreach (var interactable in interactables)
            {
                if (interactable == null)
                {
                    Debug.LogWarning($"[RiflessivoFeatures] Un componente nullo trovato su {col.gameObject.name} — saltato.");
                    continue;
                }

                Debug.Log($"    - {interactable.GetType().Name} (enabled={interactable.enabled})");

                if (interactable.enabled)
                {
                    interactable.enabled = false;
                    disabledInteractables.Add(interactable);
                    Debug.Log($"      → DISATTIVATO: {interactable.name}");
                }
                else
                {
                    Debug.Log($"      → Già disattivato: {interactable.name}");
                }
            }
        }

        Debug.Log($"[RiflessivoFeatures] Totale interattabili disattivati: {disabledInteractables.Count}");
    }



    private void EnableInteractables()
    {
        foreach (var interactable in disabledInteractables)
        {
            if (interactable != null) interactable.enabled = true;
        }
        disabledInteractables.Clear();
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

    public override void OnStepActivated(IStep step) { }
    public override void OnStepCompleted(IStep step) { }
}