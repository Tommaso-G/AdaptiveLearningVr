using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using VRBuilder.Core.Conditions;
using VRBuilder.XRInteraction.Interactables;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VisualProxy : MonoBehaviour, DynamicObjectInColliderCondition.IDynamicTargetProvider
{
    public Renderer[] renderers;
    public GameObject activeproxy;
    public GameObject CurrentTarget => activeproxy;
    public bool MultypleGrabNotAllowed = false;
    private InteractionListener interactionlistener;
    public bool isGrabbed { get; private set; }
    public event Action OnGrabbed;
    public event Action OnReleased;
    public event Action<VisualProxy> OnProxyChanged;

    private void Start()
    {
        interactionlistener = GetComponent<InteractionListener>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void setProxyWithoutGrab(GameObject proxy)
    {
        if (activeproxy != proxy)
        {
            activeproxy = proxy;
            renderers = proxy.GetComponentsInChildren<Renderer>();
            OnProxyChanged?.Invoke(this);
        }
    }

    public void releaseProxyWithoutGrab(GameObject proxy)
    {
        activeproxy = null;
        renderers = null;
        OnProxyChanged?.Invoke(this);
    }
    public void setActiveProxy(GameObject proxy)
    {

        if (activeproxy != proxy)
        {
            if (activeproxy != null && MultypleGrabNotAllowed)
            {
                InteractableObject interactable = activeproxy.GetComponent<InteractableObject>();
                var interactor = interactable.firstInteractorSelecting;
                if (interactor != null)
                {
                    interactable.interactionManager.SelectExit(interactor, interactable);
                }

            }

            activeproxy = proxy;
            renderers = proxy.GetComponentsInChildren<Renderer>();
            OnProxyChanged?.Invoke(this);
        }
        Grabbed();
    }

    public void releaseProxy(bool releaseGrabbed = true)
    {
        activeproxy = null;
        renderers = null;
        if (releaseGrabbed)
            Release();
        OnProxyChanged?.Invoke(this);
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
        OnReleased?.Invoke();
    }
}
