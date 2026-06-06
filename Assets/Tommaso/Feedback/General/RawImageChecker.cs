using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RawImageChildChecker : MonoBehaviour
{
    [Tooltip("L'oggetto da attivare se viene trovato un figlio attivo con RawImage")]
    public GameObject objectToActivate;

    private void OnEnable()
    {
        StartCoroutine(CheckNextFrame());
    }

    private IEnumerator CheckNextFrame()
    {
        yield return null; // aspetta un frame
        yield return null; // aspetta un frame
        CheckForActiveRawImageInChildren();
    }

    private void CheckForActiveRawImageInChildren()
    {
        bool found = false;

        foreach (Transform child in transform)
        {
            bool hasRawImage = child.GetComponent<RawImage>() != null;
            Debug.Log($"[RawImageChecker] Figlio '{child.name}' — attivo: {child.gameObject.activeSelf}, ha RawImage: {hasRawImage}");

            if (child.gameObject.activeSelf && hasRawImage)
            {
                found = true;
                break;
            }
        }

        if (objectToActivate != null)
        {
            Debug.Log($"[RawImageChecker] '{gameObject.name}' → SetActive({found}) su '{objectToActivate.name}'", objectToActivate);
            objectToActivate.SetActive(found);
        }
        else
        {
            
        }
    }
}