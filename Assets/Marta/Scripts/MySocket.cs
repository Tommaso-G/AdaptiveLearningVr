using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRBuilder.XRInteraction.Interactables;
public class MySocket : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; }
    [SerializeField]
    private Collider objToSnap;
    private MeshRenderer hoverRenderer;
    //[SerializeField]
    //private SlidingTilesMgr mgr;
    public bool filled { get; private set; }

    private void Start()
    {
        hoverRenderer = GetComponent<MeshRenderer>();
    }
    void OnTriggerStay(Collider other)
    {
        if (other != objToSnap) return;

        if (hoverRenderer == null)
        {
            hoverRenderer = GetComponent<MeshRenderer>();
        }

        hoverRenderer.enabled = true;
        InteractableObject interactable = other.gameObject.GetComponent<InteractableObject>();
        Transform currentParent = interactable.transform.parent;
        if (currentParent == null) return;

        IXRInteractor interactor = currentParent.GetComponent<IXRInteractor>();
        if (interactor == null)
        {
            interactable.enabled = false;
            GetComponent<Collider>().enabled = false;
            other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            other.transform.position = transform.position;
            other.transform.rotation = transform.rotation;
            hoverRenderer.enabled = false;
            filled = true;
            IsCompleted = true;
            //mgr.FormOnTarget();
        }

    }

    void OnTriggerExit(Collider other)
    {
        hoverRenderer.enabled = false;
    }

}
