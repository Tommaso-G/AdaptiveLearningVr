using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memorizza la posizione iniziale di una lista di oggetti e,
/// se escono dal collider di riferimento, li riporta alla posizione
/// di partenza con un'animazione smooth.
/// Durante il ritorno imposta il Rigidbody come isKinematic (se presente).
/// Compatibile con collider normali e trigger (isTrigger = true/false).
/// </summary>
public class ObjectBoundsKeeper : MonoBehaviour
{
    [Header("Oggetti da monitorare")]
    public List<GameObject> trackedObjects = new List<GameObject>();

    [Header("Collider di confine")]
    [Tooltip("Il collider entro cui gli oggetti devono rimanere. Funziona sia con Is Trigger attivo che disattivato.")]
    public Collider boundsCollider;

    [Header("Impostazioni animazione ritorno")]
    [Tooltip("Durata in secondi dell'animazione di ritorno.")]
    public float returnDuration = 0.5f;

    [Tooltip("Curva di easing per l'animazione di ritorno.")]
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Secondi di attesa prima di avviare il ritorno, una volta uscito dal collider.")]
    public float returnDelay = 0f;

    // Posizioni iniziali memorizzate per ogni oggetto
    readonly Dictionary<GameObject, Vector3> m_StartPositions = new Dictionary<GameObject, Vector3>();

    // Tiene traccia delle coroutine attive per evitare sovrapposizioni
    readonly Dictionary<GameObject, Coroutine> m_ActiveCoroutines = new Dictionary<GameObject, Coroutine>();

    // Memorizza lo stato isKinematic originale prima di modificarlo
    readonly Dictionary<GameObject, bool> m_OriginalKinematicState = new Dictionary<GameObject, bool>();

    void OnEnable()
    {
        m_StartPositions.Clear();

        foreach (var obj in trackedObjects)
        {
            if (obj == null) continue;
            m_StartPositions[obj] = obj.transform.position;
        }
    }

    void Update()
    {
        if (boundsCollider == null) return;

        foreach (var obj in trackedObjects)
        {
            if (obj == null) continue;

            bool isOutside = IsOutsideBounds(obj);

            if (isOutside && !m_ActiveCoroutines.ContainsKey(obj))
            {
                var coroutine = StartCoroutine(ReturnToStartRoutine(obj));
                m_ActiveCoroutines[obj] = coroutine;
            }
        }
    }

    /// <summary>
    /// Restituisce true se l'oggetto è fuori dal boundsCollider.
    /// Usa Physics.ClosestPoint che funziona indipendentemente da isTrigger.
    /// </summary>
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

    /// <summary>
    /// Imposta isKinematic sull'oggetto, salvando lo stato originale.
    /// Non fa nulla se l'oggetto non ha un Rigidbody.
    /// </summary>
    void SetKinematic(GameObject obj, bool kinematic)
    {
        if (!obj.TryGetComponent<Rigidbody>(out var rb)) return;

        if (kinematic)
        {
            // Salva lo stato originale solo la prima volta
            if (!m_OriginalKinematicState.ContainsKey(obj))
                m_OriginalKinematicState[obj] = rb.isKinematic;

            rb.isKinematic = true;
        }
        else
        {
            // Ripristina lo stato originale
            if (m_OriginalKinematicState.TryGetValue(obj, out var originalState))
            {
                rb.isKinematic = originalState;
                m_OriginalKinematicState.Remove(obj);
            }
        }
    }

    IEnumerator ReturnToStartRoutine(GameObject obj)
    {
        // Imposta kinematic subito, prima del delay
        SetKinematic(obj, true);

        if (returnDelay > 0f)
            yield return new WaitForSeconds(returnDelay);

        if (!m_StartPositions.TryGetValue(obj, out var startPos))
        {
            SetKinematic(obj, false);
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

        // Ripristina lo stato kinematic originale
        SetKinematic(obj, false);

        m_ActiveCoroutines.Remove(obj);
    }

    /// <summary>
    /// Aggiorna manualmente la posizione di partenza di un oggetto specifico.
    /// </summary>
    public void SaveCurrentPosition(GameObject obj)
    {
        if (obj != null)
            m_StartPositions[obj] = obj.transform.position;
    }

    /// <summary>
    /// Aggiorna le posizioni di partenza di tutti gli oggetti tracciati.
    /// </summary>
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
        // Ripristina kinematic e ferma le coroutine attive
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

        m_ActiveCoroutines.Clear();
        m_OriginalKinematicState.Clear();
    }
}