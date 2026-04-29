using UnityEngine;

public class ColliderTimer : MonoBehaviour
{
    [Header("Configurazione")]
    [Tooltip("L'oggetto da monitorare")]
    public GameObject targetObject;

    [Tooltip("Il collider iniziale che funge da trigger")]
    public Collider initialTriggerCollider;

    public FirePropagationEvaluator propagationEvaluator;


    [Header("Stato Timer")]
    [SerializeField] private float elapsedTime = 0f;
    [SerializeField] private bool isRunning = false;

    private void Start()
    {
        // Assicuriamoci che il collider sia impostato come trigger
        if (initialTriggerCollider!= null && initialTriggerCollider.isTrigger)
        {
            initialTriggerCollider.isTrigger = true;
            Debug.LogWarning("[ColliderTimer] Il collider iniziale è stato impostato come trigger automaticamente.");
        }

        if (targetObject == null)
            Debug.LogError("[ColliderTimer] Nessun oggetto target assegnato!");

        if (initialTriggerCollider== null)
            Debug.LogError("[ColliderTimer] Nessun trigger collider iniziale assegnato!");
    }

    private void Update()
    {
        if (isRunning)
            elapsedTime += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetObject == null) return;

        if (other.gameObject == targetObject)
        {
            StartTimer();
        }
    }


    // --- API pubblica ---

    public void StartTimer()
    {
        isRunning = true;
        Debug.Log("[ColliderTimer] Timer avviato.");
    }

    public void StopTimer()
    {
        isRunning = false;

        if (propagationEvaluator != null)
            elapsedTime += propagationEvaluator.GetGlobalPropagationLevel();

        Debug.Log($"[ColliderTimer] Timer fermato. Tempo finale (con propagazione): {elapsedTime}");
    }
    public void ResetTimer()
    {
        isRunning = false;
        elapsedTime = 0f;
        Debug.Log("[ColliderTimer] Timer resettato.");
    }

    /// <summary>
    /// Leggi il valore del timer da qualsiasi script con:
    /// GetComponent<ColliderTimer>().GetTime()
    /// </summary>
    public float GetTime() => elapsedTime;

    public bool IsRunning() => isRunning;
}