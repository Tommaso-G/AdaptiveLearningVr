// InteractionSourceAutoAttacher.cs
using UnityEngine;
using System;
public class InteractionSourceAutoAttacher : MonoBehaviour
{
    public static (bool, GameObject) AttachIfNeeded(GameObject go)
    {
        if (go.GetComponent<InteractionSource>() != null)
        {
            Debug.Log($"[AttachIfNeeded] {go.name} ha già un iteraction source: {go.GetComponent<InteractionSource>().GetType().Name}");
            return (true, go);
        }

        Component fallbackComp = null;
        Type fallbackSourceType = null;

        foreach (var componentType in InteractionSourceRegistry.RegisteredComponentTypes)
        {
            if (!InteractionSourceRegistry.TryGetSourceType(componentType, out var sourceType)) continue;

            // Priorità: cerca prima sul gameObject stesso
            var directComp = go.GetComponent(componentType);
            if (directComp != null)
            {
                Debug.Log($"[AttactListener] Per {go.name} trovato match type: {directComp.GetType().Name}.");
                if (go.GetComponent(sourceType) != null) continue;
                go.AddComponent(sourceType);
                return (true, go);
            }

            // Fallback: cerca nei parent, salva ma non attaccare ancora
            if (fallbackComp != null) continue; // tieni solo il primo trovato
            var parentComp = go.GetComponentInParent(componentType, includeInactive: true);
            if (parentComp != null && parentComp.gameObject != go)
            {
                fallbackComp = parentComp;
                fallbackSourceType = sourceType;
            }
        }

        // Nessun match diretto — usa il fallback sul parent se disponibile
        if (fallbackComp != null)
        {
            var target = fallbackComp.gameObject;
            Debug.Log($"[AttactListener] Per {go.name} trovato PARENT match type: {fallbackComp.GetType().Name}.");
            if (target.GetComponent(fallbackSourceType) == null)
                target.AddComponent(fallbackSourceType);
            return (true, target);
        }

        Debug.Log($"[InteractionSourceAutoAttacher] Non trovata una Interaction Source compatibile con {go.name}.");
        return (false, null);
    }
}