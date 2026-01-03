using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;

public class VisualProxy : MonoBehaviour
{
    public Renderer[] renderers;
    public GameObject activeproxy;
    private InteractionListener interactionlistener;
    public bool isGrabbed { get; private set; }
    public event Action OnGrabbed;

    private void Start()
    {
        interactionlistener = GetComponent<InteractionListener>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
