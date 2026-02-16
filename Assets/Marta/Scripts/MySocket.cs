using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRBuilder.XRInteraction.Interactables;
public class MySocket : MonoBehaviour
{
    [SerializeField]
    private Collider objToSnap;
    private MeshRenderer hoverRenderer;

    private void Start()
    {
        hoverRenderer = GetComponent<MeshRenderer>();
    }
    void OnTriggerStay(Collider other)
    {
        if (other != objToSnap) return;

        hoverRenderer.enabled = true;
        InteractableObject interactable = other.gameObject.GetComponent<InteractableObject>();
        Transform currentParent = interactable.transform.parent;
        IXRInteractor controller = currentParent.GetComponent<IXRInteractor>();
        if (controller == null)
        {
            interactable.enabled = false;
            GetComponent<Collider>().enabled = false;
            other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            other.transform.position = transform.position;
            other.transform.rotation = transform.rotation;
            hoverRenderer.enabled = false;
        }

    }

    void OnTriggerExit(Collider other)
    {
        hoverRenderer.enabled = false;
    }

}
