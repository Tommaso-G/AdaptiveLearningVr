using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class FireBurnDamage : MonoBehaviour
{
    public ExecutionOrderController executionOrderController;
    public GameObject objectToFlash;
    [Header("A quale capitolo registrare l'errore")]
    public string chapterErrorName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        executionOrderController = FindAnyObjectByType<ExecutionOrderController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            string chapterName = ErrorEvent.process.Data.Current.Data.Name;
            string stepName = ErrorEvent.process.Data.Current.Data.Current.Data.Name;

            if (chapterName != null)
            {
                ErrorEvent.OnError.Invoke(chapterErrorName, stepName, transform.name);
            }
            else
            {
                ErrorEvent.OnError.Invoke(chapterName, stepName, transform.name);
            }

                objectToFlash = other.gameObject.GetComponentInChildren<HapticImpulsePlayer>().gameObject;

            if (executionOrderController != null && objectToFlash != null)
                executionOrderController.DifferentStepWarningHighlight(objectToFlash);
        }
    }
}
