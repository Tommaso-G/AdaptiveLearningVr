using UnityEngine;
using UnityEngine.Events;

public class FunctionExecutor : MonoBehaviour
{
    public UnityEvent function;

    public void Execute()
    {
        function?.Invoke();
    }
}