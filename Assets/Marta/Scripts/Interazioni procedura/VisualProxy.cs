using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using VRBuilder.Core.Conditions;

public class VisualProxy : MonoBehaviour, DynamicObjectInColliderCondition.IDynamicTargetProvider
{
    public Renderer[] renderers;
    public GameObject activeproxy;
    public GameObject CurrentTarget => activeproxy;
    private InteractionListener interactionlistener;
    public bool isGrabbed { get; private set; }
    public event Action OnGrabbed;

    private void Start()
    {
        interactionlistener = GetComponent<InteractionListener>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void setDisableProxy(GameObject proxy)
    {
        if (!proxy.gameObject.activeSelf)
        {
            setActiveProxy(proxy);
        }
    }

    public void releaseDisableProxy(GameObject proxy)
    {
        if (!proxy.gameObject.activeSelf)
        {
            releaseProxy(true);
        }
    }
    public void setActiveProxy(GameObject proxy)
    {
        activeproxy = proxy;
        renderers = proxy.GetComponentsInChildren<Renderer>();
        Grabbed();
    }

    public void releaseProxy(bool releaseGrabbed = true)
    {
        activeproxy = null;
        renderers = null;
        if (releaseGrabbed)
        {
            Release();
        }
    }

    public Renderer[] GetProxyRenderers()
    {
        return renderers;
    }

    public void Grabbed()
    {
        isGrabbed = true;
        OnGrabbed?.Invoke();
    }

    public void Release()
    {
        isGrabbed = false;
    }
}
