using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core.Properties;

public class VisualProxyInteractionSource : InteractionSource
{
    private VisualProxy proxy;
    public override bool CanHandle(GameObject obj)
    => obj.TryGetComponent<VisualProxy>(out _);
    private void Awake()
    {
        proxy = GetComponent<VisualProxy>();
    }

    private void OnEnable()
    {
        proxy.OnGrabbed += OnGrabbedProxy;
    }

    private void OnDisable()
    {
        proxy.OnGrabbed -= OnGrabbedProxy;
    }
    private void OnGrabbedProxy()
    {
        RaiseInteraction(
            InteractionKind.Proxy,
            gameObject,
            context: proxy.activeproxy
        );
    }
}
