using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core.Properties;

public class DoorInteractionSource : InteractionSource
{
    private ClosableDoor door;

    public override bool CanHandle(GameObject obj)
    => obj.TryGetComponent<ClosableDoor>(out _);

    private void Awake()
    {
        door = GetComponent<ClosableDoor>();
        errorString = "Porta";
    }

    private void OnEnable()
    {
        door.onDoorClosed.AddListener(OnDoorClosed);
    }

    private void OnDisable()
    {
        door.onDoorClosed.RemoveListener(OnDoorClosed);
    }
    private void OnDoorClosed()
    {
        RaiseInteraction(
             InteractionKind.Door,
             gameObject
         );
    }
}