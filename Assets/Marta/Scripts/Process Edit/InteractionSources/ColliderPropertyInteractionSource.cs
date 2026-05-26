using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core.Properties;

public class ColliderPropertyInteractionSource : InteractionSource
{
    [SerializeField]
    private string requiredTag = "Player";
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<ColliderWithTriggerProperty>(out _);

    private void Awake()
    {
        errorString = "Aula";
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        RaiseInteraction(
            InteractionKind.Trigger,
            gameObject,
            interactor: other.gameObject
        );
    }
}
