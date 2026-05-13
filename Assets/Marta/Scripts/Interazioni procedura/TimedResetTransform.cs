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

        timerCoroutine = StartCoroutine(ResetAfterDelay());
    }

    public void StopTimer()
    {
        print("[TimedResetTransform] StopTimer");
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        ResetTransform();
    }

    public void ResetTransform()
    {
        print("[TimedResetTransform] ResetTransform inizio");
        if (rb != null)
        {
            if (resetRigidbodyVelocity)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
        }

        if (targetPosition != null && targetRotation != null)
        {
            print("[TimedResetTransform] TargetTransform reset");
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            print("[TimedResetTransform] TargetTransform initial");
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        if (rb != null)
            rb.isKinematic = true;
    }
}