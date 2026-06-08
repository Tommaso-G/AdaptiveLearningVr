using UnityEngine;
using System.Collections;

public class TimedResetTransform : MonoBehaviour
{
    [Header("Timer Settings")]
    public float resetDelay = 5f;
    public bool startTimerOnEnable = false;

    [Header("Target (optional override)")]
    public Transform targetTransform; // se nullo usa posizione iniziale
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    [Header("Physics")]
    public bool resetRigidbodyVelocity = true;

    [Header("Colliders")]
    [SerializeField] private Transform transformWithColliders;

    [Header("Visual Reset")]
    [SerializeField] private Transform transformWithMeshes;
    [SerializeField] private float meshEnableDelay = 0.2f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private Coroutine timerCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (targetTransform == null)
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
    }
    void Start()
    {
        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
            targetRotation = targetTransform.rotation;
        }
    }

    void OnEnable()
    {
        if (startTimerOnEnable)
            StartTimer();
    }

    public void StartTimer()
    {
        print("[TimedResetTransform] StartTimer");
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(ResetRoutine());
    }

    public void StopTimer()
    {
        print("[TimedResetTransform] StopTimer");
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (transformWithColliders == null)
            return;

        Collider[] colliders =
            transformWithColliders.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            col.enabled = enabled;
        }
    }

    private void SetMeshesEnabled(bool enabled)
    {
        if (transformWithMeshes == null)
            return;

        foreach (var mr in transformWithMeshes.GetComponentsInChildren<MeshRenderer>(true))
            mr.enabled = enabled;

        foreach (var smr in transformWithMeshes.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.enabled = enabled;
    }
    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(resetDelay);
        // Nasconde le mesh
        SetMeshesEnabled(false);

        // Disabilita collider
        SetCollidersEnabled(false);

        // Blocca fisica
        if (rb != null)
        {
            rb.isKinematic = true;

            if (resetRigidbodyVelocity)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Ripristina posizione e rotazione globali
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Riabilita collider
        SetCollidersEnabled(true);

        // Attende un tempo configurabile
        yield return new WaitForSeconds(meshEnableDelay);

        // Riabilita mesh
        SetMeshesEnabled(true);
    }
}