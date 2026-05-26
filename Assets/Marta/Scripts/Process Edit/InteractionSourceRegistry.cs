// InteractionSourceRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using VRBuilder.Core.Properties;

public static class InteractionSourceRegistry
{
    // Mappa: tipo del componente interagibile → tipo dell'InteractionSource da aggiungere
    private static readonly Dictionary<Type, Type> _map = new()
    {
        { typeof(XRPushButtonVrBuilder),  typeof(XRButtonInteractionSource) },
        { typeof(ClosableDoor),                 typeof(DoorInteractionSource) },
        { typeof(XRLever),                typeof(LeverInteractionSource) },
        { typeof(Button),                typeof(UIButtonInteractionSource) },
        { typeof(VisualProxy),                typeof(VisualProxyInteractionSource) },
        { typeof(XRBaseInteractable),                typeof(XRBaseInteractableInteractionSource) },
        { typeof(ColliderWithTriggerProperty),    typeof(ColliderPropertyInteractionSource) },
        { typeof(FollowerAgentWithCheck),   typeof(FollowerAgentInteractionSource)},
        // aggiungi qui nuove coppie
    };

    public static bool TryGetSourceType(Type componentType, out Type sourceType)
        => _map.TryGetValue(componentType, out sourceType);

    public static IEnumerable<Type> RegisteredComponentTypes => _map.Keys;
}