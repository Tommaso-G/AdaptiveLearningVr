using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core.Properties;

public class LeverInteractionSource : InteractionSource
{
    private XRLever lever;

    public override bool CanHandle(GameObject obj)
    => obj.TryGetComponent<XRLever>(out _);
    private void Awake()
    {
        lever = GetComponent<XRLever>();
    }

    private void OnEnable()
    {
        lever.onLeverActivate.AddListener(OnLeverActivated);
    }

    private void OnDisable()
    {
        lever.onLeverActivate.RemoveListener(OnLeverActivated);
    }
    private void OnLeverActivated()
    {
        RaiseInteraction(
            InteractionKind.Lever,
            gameObject
        );
    }
}