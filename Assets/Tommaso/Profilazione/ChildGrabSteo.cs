using System.Linq;
using UnityEngine;
using VRBuilder.Core.Properties;
using VRBuilder.XRInteraction.Properties;

public class ChildGrabStep : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; }

    private GrabbableProperty[] grabbableChildren;

    private void Awake()
    {
        // Cerca tutti i figli con GrabbableProperty
        grabbableChildren = GetComponentsInChildren<GrabbableProperty>(true);
    }

    private void Update()
    {
        if (IsCompleted)
            return;

        // Controlla se almeno uno dei figli è stato grabbato
        foreach (var grabbable in grabbableChildren)
        {
            if (grabbable != null && grabbable.IsGrabbed)
            {
                IsCompleted = true;
                Debug.Log($"Step completato: {grabbable.gameObject.name} è stato grabbato.");
                break;
            }
        }
    }
}