using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
public class KinematicAndLimitedInteractionMask : MonoBehaviour
{
    [Header("Collider di riferimento")]
    public Collider targetTrigger;

    [Header("Layer XR aggiuntivo dentro il collider")]
    [Tooltip("Nome dell'Interaction Layer XR da aggiungere temporaneamente insieme a 'Default'")]
    public string extraLayerName = "Snapped";

    private XRGrabInteractable grab;
    private Rigidbody rb;

    private InteractionLayerMask originalMask;
    private InteractionLayerMask maskInside;

    private bool isInsideTrigger = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalMask = grab.interactionLayers;

        var defaultMask = InteractionLayerMask.GetMask("Default");
        var extraMask = InteractionLayerMask.GetMask(extraLayerName);

        if (extraMask == 0)
            Debug.LogWarning($"⚠️ Interaction Layer '{extraLayerName}' non trovato. Verrà usato solo 'Default'.");

        maskInside = defaultMask | extraMask;

        // Aggiungiamo listener per garantire che l'oggetto torni fisico dopo il rilascio
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == targetTrigger)
        {
            isInsideTrigger = true;
            rb.isKinematic = true;
            grab.interactionLayers = maskInside;
           // Debug.Log($"{name}: entrato in {other.name} → Rigidbody kinematic, XR mask = {MaskToString(maskInside)}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == targetTrigger)
        {
            isInsideTrigger = false;
            rb.isKinematic = false;
            grab.interactionLayers = originalMask;
            //Debug.Log($"{name}: uscito da {other.name} → Rigidbody fisico, XR mask ripristinati = {MaskToString(originalMask)}");
        }
    }

    // 🔧 Quando l’oggetto viene rilasciato manualmente
    private void OnReleased(SelectExitEventArgs args)
    {
        if (!isInsideTrigger)
        {
            // Se non è nel trigger, torna sempre fisico
            rb.isKinematic = false;
            Debug.Log($"{name}: rilasciato fuori dal trigger → Rigidbody fisico attivato");
        }
    }

    // 🔍 Utility di debug leggibile
    private string MaskToString(InteractionLayerMask mask)
    {
        int value = mask.value;
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & value) != 0)
                result += LayerMask.LayerToName(i) + " ";
        }
        return result.Trim();
    }
}