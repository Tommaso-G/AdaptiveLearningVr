using UnityEngine;
using UnityEngine.UI;

public class RawImageChildChecker : MonoBehaviour
{
    [Tooltip("L'oggetto da attivare se viene trovato un figlio attivo con RawImage")]
    public GameObject objectToActivate;

    private void OnEnable()
    {
        CheckForActiveRawImageInChildren();
    }

    private void CheckForActiveRawImageInChildren()
    {
        bool found = false;

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf && child.GetComponent<RawImage>() != null)
            {
                found = true;
                break;
            }
        }

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(found);
        }
        else
        {
            Debug.LogWarning("[RawImageChildChecker] Nessun oggetto assegnato in 'objectToActivate'.");
        }
    }
}