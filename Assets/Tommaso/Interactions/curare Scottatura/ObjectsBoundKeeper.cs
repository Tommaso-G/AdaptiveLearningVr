using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Memorizza la posizione iniziale di una lista di oggetti e,
/// se escono dal collider di riferimento, li riporta alla posizione
/// di partenza con un'animazione smooth.
/// Durante il ritorno:
/// - imposta il Rigidbody come isKinematic
/// - disattiva tutti i Collider dell'oggetto e dei figli
/// Al termine:
/// - ripristina isKinematic
/// - riattiva i Collider precedentemente attivi
/// Compatibile con collider normali e trigger.
/// </summary>
public class ObjectBoundsKeeper : MonoBehaviour
{
    [Header("Oggetti da monitorare")]
    public List<GameObject> trackedObjects = new List<GameObject>();

    [Header("Collider di confine")]
    [Tooltip("Il collider entro cui gli oggetti devono rimanere.")]
    public Collider boundsCollider;

    [Header("Impostazioni animazione ritorno")]
    [Tooltip("Durata in secondi dell'animazione di ritorno.")]
    public float returnDuration = 0.5f;

    [Tooltip("Curva di easing per l'animazione di ritorno.")]
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Secondi di attesa prima di avviare il ritorno.")]
    public float returnDelay = 0f;

    [Header("Debug")]
    public bool debugMode = false;

    readonly Dictionary<GameObject, Vector3> m_StartPositions = new();
    readonly Dictionary<GameObject, Coroutine> m_ActiveCoroutines = new();
    readonly Dictionary<GameObject, bool> m_OriginalKinematicState = new();

    // Salva lo stato originale dei collider
    readonly Dictionary<GameObject, List<ColliderState>> m_OriginalColliderStates = new();

    class ColliderState
    {
        public Collider collider;
        public bool wasEnabled;
    }

    void OnEnable()
    {
        m_StartPositions.Clear();

        foreach (var obj in trackedObjects)
        {
            if (obj == null) continue;

            m_StartPositions[obj] = obj.transform.position;

            if (debugMode)
                Debug.Log($"[ObjectBoundsKeeper] Posizione salvata per '{obj.name}': {obj.transform.position}", obj);
        }

        if (boundsCollider == null)
            Debug.LogWarning("[ObjectBoundsKeeper] Nessun boundsCollider assegnato!", this);
    }

    void Update()
    {
        if (boundsCollider == null) return;

        foreach (var obj in trackedObjects)
        {
            if (obj == null) continue;

            bool isOutside = IsOutsideBounds(obj);
            bool coroutineRunning = m_ActiveCoroutines.ContainsKey(obj);

            if (isOutside && !coroutineRunning)
            {
                if (debugMode)
                    Debug.Log($"[ObjectBoundsKeeper] Avvio ritorno per '{obj.name}'", obj);

                var coroutine = StartCoroutine(ReturnToStartRoutine(obj));
                m_ActiveCoroutines[obj] = coroutine;
            }
        }
    }

    bool IsOutsideBounds(GameObject obj)
    {
        var point = obj.transform.position;

        var closest = Physics.ClosestPoint(
            point,
            boundsCollider,
            boundsCollider.transform.position,
            boundsCollider.transform.rotation
        );

        return (closest - point).sqrMagnitude > 1e-6f;
    }

    void SetKinematic(GameObject obj, bool kinematic)
    {
        var rb = obj.GetComponentInChildren<Rigidbody>();

        if (rb == null)
        {
            if (debugMode)
                Debug.Log($"[ObjectBoundsKeeper] Nessun Rigidbody trovato su '{obj.name}'", obj);

            return;
        }

        if (kinematic)
        {
            if (!m_OriginalKinematicState.ContainsKey(obj))
                m_OriginalKinematicState[obj] = rb.isKinematic;

            rb.isKinematic = true;
        }
        else
        {
            if (m_OriginalKinematicState.TryGetValue(obj, out var originalState))
            {
                rb.isKinematic = originalState;
                m_OriginalKinematicState.Remove(obj);
            }
        }
    }

    /// <summary>
    /// Disattiva tutti i collider dell'oggetto e dei figli
    /// salvando il loro stato originale.
    /// </summary>
    void SetCollidersEnabled(GameObject obj, bool enabled)
    {
        var colliders = obj.GetComponentsInChildren<Collider>(true);

        if (!enabled)
        {
            if (!m_OriginalColliderStates.ContainsKey(obj))
                m_OriginalColliderStates[obj] = new List<ColliderState>();

            m_OriginalColliderStates[obj].Clear();

            foreach (var col in colliders)
            {
                m_OriginalColliderStates[obj].Add(new ColliderState
                {
                    collider = col,
                    wasEnabled = col.enabled
                });

                col.enabled = false;
            }

            if (debugMode)
                Debug.Log($"[ObjectBoundsKeeper] Collider disattivati per '{obj.name}'", obj);
        }
        else
        {
            if (!m_OriginalColliderStates.TryGetValue(obj, out var states))
                return;

            foreach (var state in states)
            {
                if (state.collider != null)
                    state.collider.enabled = state.wasEnabled;
            }

            m_OriginalColliderStates.Remove(obj);

            if (debugMode)
                Debug.Log($"[ObjectBoundsKeeper] Collider ripristinati per '{obj.name}'", obj);
        }
    }

    IEnumerator ReturnToStartRoutine(GameObject obj)
    {
        SetKinematic(obj, true);

        // Disattiva collider durante il ritorno
        SetCollidersEnabled(obj, false);

        if (returnDelay > 0f)
            yield return new WaitForSeconds(returnDelay);

        if (!m_StartPositions.TryGetValue(obj, out var startPos))
        {
            Debug.LogWarning($"[ObjectBoundsKeeper] Posizione iniziale non trovata per '{obj.name}'", obj);

            SetKinematic(obj, false);
            SetCollidersEnabled(obj, true);

            m_ActiveCoroutines.Remove(obj);
            yield break;
        }

        var fromPos = obj.transform.position;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / returnDuration);
            float curvedT = returnCurve.Evaluate(t);

            obj.transform.position = Vector3.Lerp(fromPos, startPos, curvedT);

            yield return null;
        }

        obj.transform.position = startPos;

        // Ripristina stato finale
        SetKinematic(obj, false);
        SetCollidersEnabled(obj, true);

        m_ActiveCoroutines.Remove(obj);

        if (debugMode)
            Debug.Log($"[ObjectBoundsKeeper] '{obj.name}' è tornato alla posizione iniziale.", obj);
    }

    public void SaveCurrentPosition(GameObject obj)
    {
        if (obj != null)
            m_StartPositions[obj] = obj.transform.position;
    }

    public void SaveAllCurrentPositions()
    {
        foreach (var obj in trackedObjects)
        {
            if (obj != null)
                m_StartPositions[obj] = obj.transform.position;
        }
    }

    void OnDisable()
    {
        foreach (var kvp in m_ActiveCoroutines)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }

        foreach (var obj in new List<GameObject>(m_OriginalKinematicState.Keys))
        {
            if (obj != null)
                SetKinematic(obj, false);
        }

        foreach (var obj in new List<GameObject>(m_OriginalColliderStates.Keys))
        {
            if (obj != null)
                SetCollidersEnabled(obj, true);
        }

        m_ActiveCoroutines.Clear();
        m_OriginalKinematicState.Clear();
        m_OriginalColliderStates.Clear();
    }
}