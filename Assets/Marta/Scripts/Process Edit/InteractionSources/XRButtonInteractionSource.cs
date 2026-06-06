using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class XRButtonInteractionSource : InteractionSource
{
    private XRPushButton button;
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<XRPushButton>(out _);
    private void Awake()
    {
        button = GetComponent<XRPushButton>();
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