using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permette di mettere tutti i collider (oggetto, figli e aggiuntivi)
/// in modalità isTrigger e poi ripristinarli allo stato originale.
/// </summary>
public class ColliderToggler : MonoBehaviour
{
    [Header("Collider aggiuntivi")]
    [Tooltip("Collider extra da includere.")]
    public List<Collider> additionalColliders = new();

    [Header("Debug")]
    public bool debugMode = false;

    class ColliderState
    {
        public Collider collider;
        public bool wasTrigger;
    }

    readonly List<ColliderState> m_SavedStates = new();
    bool m_IsTriggerMode = false;

    /// <summary>
    /// Imposta tutti i collider in isTrigger e salva lo stato originale.
    /// </summary>
    public void EnableTriggerMode()
    {
        if (m_IsTriggerMode)
        {
            if (debugMode)
                Debug.Log($"[ColliderToggler] '{name}': già in trigger mode, skip.", this);

            return;
        }

        m_SavedStates.Clear();

        HashSet<Collider> colliders = new();

        foreach (var col in GetComponentsInChildren<Collider>(true))
            if (col != null) colliders.Add(col);

        foreach (var col in additionalColliders)
            if (col != null) colliders.Add(col);

        foreach (var col in colliders)
        {
            m_SavedStates.Add(new ColliderState
            {
                collider = col,
                wasTrigger = col.isTrigger
            });

            col.isTrigger = true;
        }

        m_IsTriggerMode = true;

        if (debugMode)
            Debug.Log($"[ColliderToggler] '{name}': {colliders.Count} collider messi in isTrigger.", this);
    }

    /// <summary>
    /// Ripristina lo stato originale dei collider.
    /// </summary>
    public void DisableTriggerMode()
    {
        if (!m_IsTriggerMode)
        {
            if (debugMode)
                Debug.Log($"[ColliderToggler] '{name}': non in trigger mode, skip.", this);

            return;
        }

        int restored = 0;

        foreach (var state in m_SavedStates)
        {
            if (state.collider == null)
                continue;

            state.collider.isTrigger = state.wasTrigger;
            restored++;
        }

        m_SavedStates.Clear();
        m_IsTriggerMode = false;

        if (debugMode)
            Debug.Log($"[ColliderToggler] '{name}': {restored} collider ripristinati.", this);
    }

    void OnDisable()
    {
        // Sicurezza: ripristina sempre
        if (m_IsTriggerMode)
            DisableTriggerMode();
    }
}