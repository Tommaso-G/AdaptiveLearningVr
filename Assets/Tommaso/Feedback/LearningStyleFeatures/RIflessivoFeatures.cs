using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core;

[CreateAssetMenu(menuName = "Learning Styles/Riflessivo Behaviour")]
public class RiflessivoFeatures : LearningStyleFeatures
{
    private Volume globalVolume;

    public float audioFadeDuration = 1.5f; // durata fade audio
    [SerializeField] private float interactionRadius = 5f; // raggio d'azione
    [SerializeField] private LayerMask interactableLayer;  // layer degli oggetti interagibili

    // Mantiene traccia solo degli oggetti disabilitati da questo effetto
    private readonly List<XRBaseInteractable> disabledInteractables = new();

    public override void OnFeedbackOpened(FeedbackPrefabController feedback)
    {
        if (feedback == null) return;

        Debug.Log($"[RiflessivoFeatures] Feedback '{feedback.name}' aperto — inizio effetto riflessivo.");

        if (feedback.WasClicked)
        {
            Debug.Log($"[RiflessivoFeatures] Feedback '{feedback.name}' è già stato cliccato — nessun effetto.");
            return;
        }

        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();

        if (globalVolume != null)
            feedback.StartCoroutine(FadeVolumeWeight(globalVolume, 1f, 0.5f));

        feedback.StartCoroutine(FadeAudioVolume(1f, 0f, audioFadeDuration));

        // Disattiva interagibili vicini al feedback
        DisableInteractable(feedback.transform.position, interactionRadius, interactableLayer);

        // Pausa temporale globale
        Time.timeScale = 0;
    }

    public override void OnFeedbackClosed(FeedbackPrefabController feedback)
    {
        if (feedback == null) return;

        Debug.Log($"[RiflessivoFeatures] Feedback '{feedback.name}' chiuso — ripristino ambiente.");

        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();

        if (globalVolume != null)
            feedback.StartCoroutine(FadeVolumeWeight(globalVolume, 0f, 0.5f));

        feedback.StartCoroutine(FadeAudioVolume(0f, 1f, audioFadeDuration));

        // Riattiva solo gli interagibili che erano stati disattivati
        EnableInteractable();

        // Riprende il tempo di gioco
        Time.timeScale = 1;
    }

    public void OnFeedbackClicked(FeedbackPrefabController feedback)
    {
        if (feedback == null) return;

        Debug.Log($"[RiflessivoFeatures] Feedback '{feedback.name}' cliccato — blocco effetto.");

        feedback.WasClicked = true;

        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();

        if (globalVolume != null)
            feedback.StartCoroutine(FadeVolumeWeight(globalVolume, 0f, 0.5f));
        
        EnableInteractable();

        Time.timeScale = 1;
    }

    public override void OnStepActivated(IStep step)
    {
        Debug.Log("[RiflessivoFeatures] Step attivato — modalità riflessiva attiva.");
    }

    public override void OnStepCompleted(IStep step)
    {
        Debug.Log("[RiflessivoFeatures] Step completato — modalità riflessiva terminata.");
    }

    // ------------------------------------------
    // Utility interne
    // ------------------------------------------

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
        AudioListener.volume = start;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        AudioListener.volume = end;
    }

    // ------------------------------------------
    // Gestione interagibili
    // ------------------------------------------

    private void DisableInteractable(Vector3 center, float radius, LayerMask interactableLayer)
    {
        disabledInteractables.Clear(); // pulisce la lista per questa sessione

        Collider[] nearbyObjects = Physics.OverlapSphere(center, radius, interactableLayer);
        foreach (Collider col in nearbyObjects)
        {
            Debug.Log($"[RiflessivoFeatures] trovato: {col.name}");
            XRBaseInteractable interactable = col.GetComponent<XRBaseInteractable>();
            if (interactable != null && interactable.enabled)
            {
                interactable.enabled = false;
                disabledInteractables.Add(interactable);
                Debug.Log($"[RiflessivoFeatures] Disabilitato interagibile: {interactable.name}");
            }
        }
    }

    private void EnableInteractable()
    {
        foreach (XRBaseInteractable interactable in disabledInteractables)
        {
            if (interactable != null)
            {
                interactable.enabled = true;
                Debug.Log($"[RiflessivoFeatures] Riattivato interagibile: {interactable.name}");
            }
        }

        disabledInteractables.Clear();
    }
}
