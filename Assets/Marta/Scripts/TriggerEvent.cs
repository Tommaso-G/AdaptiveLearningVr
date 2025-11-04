using UnityEngine;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour
{
    public UnityEvent onEnter;
    public UnityEvent onExit;
    public string tagToCompare;

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(tagToCompare))
        {
            if (other.CompareTag(tagToCompare))
            {
                onEnter?.Invoke();
            }
        }
        else
        {
            onEnter?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        onExit?.Invoke();
    }
}
