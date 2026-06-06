using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using VRBuilder.Core.Properties;

public static class InteractionSourceRegistry
{
    private static readonly Dictionary<Type, Type> _map = new()
    {
        { typeof(XRPushButton),          typeof(XRButtonInteractionSource) },
        { typeof(XRPushButtonVrBuilder),          typeof(VrBuilderPushButtonInteractionSource) },
        { typeof(ClosableDoor),                   typeof(DoorInteractionSource) },
        { typeof(XRLever),                        typeof(LeverInteractionSource) },
        { typeof(Button),                         typeof(UIButtonInteractionSource) },
        { typeof(VisualProxy),                    typeof(VisualProxyInteractionSource) },
        { typeof(ColliderWithTriggerProperty),    typeof(ColliderPropertyInteractionSource) },
        { typeof(FollowerAgentWithCheck),         typeof(FollowerAgentInteractionSource) },
        { typeof(XRBaseInteractable),             typeof(XRBaseInteractableInteractionSource) },
    };

    public static bool TryGetSourceType(Type componentType, out Type sourceType)
        => _map.TryGetValue(componentType, out sourceType);

    public static IEnumerable<Type> RegisteredComponentTypes => _map.Keys
        .OrderByDescending(t => Depth(t));

    public static int Depth(Type t)
    {
        int d = 0;
        while (t.BaseType != null) { t = t.BaseType; d++; }
        return d;
    }
}