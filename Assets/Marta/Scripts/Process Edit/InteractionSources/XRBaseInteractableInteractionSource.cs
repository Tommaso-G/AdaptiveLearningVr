using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core.Properties;

public class XRBaseInteractableInteractionSource : InteractionSource
{
    private XRBaseInteractable interactable;
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<XRBaseInteractable>(out _);
    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnInteraction);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnInteraction); ;
    }

    private void OnInteraction(BaseInteractionEventArgs args)
    {
        if (args.interactorObject.transform.gameObject.name == "Near-Far Interactor")
        {
            RaiseInteraction(
                InteractionKind.Interactable,
                gameObject
            );
        }
    }
}
