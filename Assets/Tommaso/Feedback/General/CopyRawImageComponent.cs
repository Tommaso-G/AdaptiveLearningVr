using UnityEngine;
using UnityEngine.UI;

public class CopyRawImageComponent : MonoBehaviour
{
    [SerializeField] private Transform sourceParent;
    [SerializeField] private RawImage target;
    // TRUE = cerca nel primo figlio attivo e nei suoi figli
    // FALSE = cerca solo tra i figli diretti
    [SerializeField] private bool searchDeep = true;

    private void Start()
    {
        if (sourceParent == null)
        {
            Debug.LogWarning("[CopyRawImage] Source Parent non assegnato.");
            return;
        }

        RawImage source = null;

        if (searchDeep)
        {
            // 1) trova primo figlio attivo
            Transform activeChild = null;

            foreach (Transform child in sourceParent)
            {
                if (child.gameObject.activeSelf)
                {
                    source = child.GetComponentInChildren<RawImage>(true);
                    if (source != null)
                        break;
                }
            }

            if (activeChild != null)
            {
                // 2) cerca RawImage nei suoi discendenti
                source = activeChild.GetComponentInChildren<RawImage>(true);
            }
        }
        else
        {
            // ricerca SOLO tra i figli diretti
            foreach (Transform child in sourceParent)
            {
                RawImage img = child.GetComponent<RawImage>();

                if (img != null)
                {
                    source = img;
                    break;
                }
            }
        }

        if (source == null)
        {
            Debug.LogWarning("[CopyRawImage] Nessuna RawImage trovata secondo la modalità selezionata.");
            return;
        }


        if (target == null)
        {
            Debug.LogWarning("[CopyRawImage] RawImage target mancante.");
            return;
        }

        JsonUtility.FromJsonOverwrite(
            JsonUtility.ToJson(source),
            target
        );
    }
}