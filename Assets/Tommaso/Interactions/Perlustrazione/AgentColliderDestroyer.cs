using UnityEngine;

/// <summary>
/// Aggiunto automaticamente da NavAgentDestinationSetter.
/// Distrugge il GameObject non appena tocca il collider target.
/// Assicurati che il collider target abbia "Is Trigger" abilitato,
/// oppure usa OnCollisionEnter se è un collider solido.
/// </summary>
public class AgentSelfDestruct : MonoBehaviour
{
    [HideInInspector]
    public Collider targetCollider;

    // Se il collider target è un Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other == targetCollider)
            Destroy(gameObject);
    }

    // Se il collider target è un collider solido (non trigger)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == targetCollider)
            Destroy(gameObject);
    }
}