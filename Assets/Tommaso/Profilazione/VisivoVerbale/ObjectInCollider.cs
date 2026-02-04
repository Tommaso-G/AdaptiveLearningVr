using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ObjectCompleteOnEnter : MonoBehaviour
{
    [Header("Collider obiettivo")]
    [Tooltip("Il collider dentro il quale questo oggetto deve entrare per completare lo step")]
    public Collider colliderTarget;

    [Header("Evento chiamato quando l'oggetto entra nel collider scelto")]
    public UnityEvent OnComplete;

    private bool completato = false;

    private void Reset()
    {
        // Assicura che questo collider NON sia trigger (di solito è l'oggetto mobile)
        GetComponent<Collider>().isTrigger = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignora se già completato o se non c'è target
        if (completato || colliderTarget == null) return;

        // Se il collider in cui è entrato è quello target
        if (other == colliderTarget)
        {
            completato = true;
            Debug.Log($"✅ {name} ha completato lo step entrando in {other.name}");
            OnComplete?.Invoke();
        }
    }
}
