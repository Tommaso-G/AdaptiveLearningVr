using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CardFlipper : MonoBehaviour
{
    [Header("Animazione")]
    public float flipDuration = 0.3f; // Durata animazione in secondi

    public bool inGame = false; 
    private bool isFlipped = false;   // true → faccia visibile (rotazione 0°)
    private bool isAnimating = false;
    private XRBaseInteractable interactable;

    public MemoryManager memoryManager; 
    public string cardID; // identità della carta (nome immagine o parola)

    

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelected);

        // Blocca la rotazione del Rigidbody per evitare interferenze XR
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        isFlipped = false;

    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        // Se la carta è già flippata o in animazione o non è ancora partito il gioco, non fare nulla
        if (isFlipped || isAnimating|| !inGame || !memoryManager.CanInteract())
            return;

        
        StartCoroutine(FlipCard(180f));

        memoryManager.OnCardSelected(this);
    }

    public IEnumerator FlipCard(float rotationAmount)
    {
        if (isAnimating)
            yield break;
        isAnimating = true;
        float elapsed = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(rotationAmount, 0f, 0f);

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flipDuration);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.rotation = endRot;
        isFlipped = !isFlipped;
        isAnimating = false;
    }

    // Normalizza l’angolo in [0,360)
    public bool IsFlipped() => isFlipped;
}