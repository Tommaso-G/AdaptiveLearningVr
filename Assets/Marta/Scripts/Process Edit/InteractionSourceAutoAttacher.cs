// InteractionSourceAutoAttacher.cs
using UnityEngine;

public class InteractionSourceAutoAttacher : MonoBehaviour
{
    public static bool AttachIfNeeded(GameObject go)
    {
        foreach (var componentType in InteractionSourceRegistry.RegisteredComponentTypes)
        {
            // Cerca il componente su go e nei suoi parent
            var comp = go.GetComponentInParent(componentType, includeInactive: true);
            if (comp == null) continue;

            if (!InteractionSourceRegistry.TryGetSourceType(componentType, out var sourceType)) continue;

            // Attacca l'InteractionSource sul GameObject che ospita il componente, non su go
            var target = comp.gameObject;

            if (target.GetComponent(sourceType) != null) continue;

            target.AddComponent(sourceType);
            return true;
        }

        Debug.Log($"[InteractionSourceAutoAttacher] Non trovata una Interaction Source compatibile con {go.name}.");
        return false;
    }
}