using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ResetSocketsOnStart : MonoBehaviour
{
    IEnumerator Start()
    {
        // Aspetta un frame per far inizializzare la griglia e i collider
        yield return null;

        // --- RESET SOCKET ---
        var sockets = Object.FindObjectsByType<XRSocketInteractor>(FindObjectsSortMode.None);
        Debug.Log($"[ResetSocketsOnStart] Found {sockets.Length} sockets.");

        foreach (var socket in sockets)
        {
            if (socket.hasSelection)
            {
                socket.EndManualInteraction();
                Debug.Log($"[ResetSocketsOnStart] Cleared socket selection: {socket.name}");
            }

            // Opzionale: se la griglia viene generata a runtime
            // socket.socketActive = false;
            // socket.socketActive = true; // puoi riattivarlo subito dopo un frame
        }

        // --- RESET INTERACTABLE ---
        var interactables = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        Debug.Log($"[ResetSocketsOnStart] Found {interactables.Length} interactables.");

        foreach (var interactable in interactables)
        {
            if (interactable.isSelected)
            {
                interactable.interactionManager.CancelInteractableSelection(interactable as IXRSelectInteractable);
                Debug.Log($"[ResetSocketsOnStart] Cleared interactable selection: {interactable.name}");
            }
        }

        Debug.Log("[ResetSocketsOnStart] Reset complete!");
    }
}
