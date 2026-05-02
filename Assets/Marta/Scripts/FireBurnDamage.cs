using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class FireBurnDamage : MonoBehaviour
{
    public ErrorReporter ErrorReporter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ErrorReporter != null)
            {
                ErrorReporter.RegisterError(gameObject.name);
            }
            else
            {
                Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
            }
        }
    }
}
