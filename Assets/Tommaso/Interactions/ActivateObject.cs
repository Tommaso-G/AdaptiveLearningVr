using UnityEngine;

public class ActivateTargetOnEnable : MonoBehaviour
{
    [Header("Oggetto da attivare")]
    [SerializeField] private GameObject targetObject;

    private void OnEnable()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }
}