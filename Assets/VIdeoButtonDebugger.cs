using UnityEngine;

public class ActivationTracer : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log($"[TRACER] '{gameObject.name}' attivato da:\n{System.Environment.StackTrace}", gameObject);
    }


}