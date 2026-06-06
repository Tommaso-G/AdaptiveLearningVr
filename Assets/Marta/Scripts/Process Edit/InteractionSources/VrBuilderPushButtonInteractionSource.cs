using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class VrBuilderPushButtonInteractionSource : InteractionSource
{
    private XRPushButtonVrBuilder button;
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<XRPushButtonVrBuilder>(out _);
    private void Awake()
    {
        button = GetComponent<XRPushButtonVrBuilder>();
    }

    private void OnEnable()
    {
        button.onPress.AddListener(OnPressed);
    }

    private void OnDisable()
    {
        button.onPress.RemoveListener(OnPressed);
    }

    private void OnPressed()
    {
        RaiseInteraction(
            InteractionKind.Button,
            gameObject
        );
    }
}