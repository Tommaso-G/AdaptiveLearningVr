using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasLayoutRefresher : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    void Start()
    {
        StartCoroutine(RefreshLayout());
    }

    private System.Collections.IEnumerator RefreshLayout()
    {
        yield return new WaitForEndOfFrame();

        // Forza il rebuild di tutti i TextMeshPro nel panel
        foreach (var tmp in targetPanel.GetComponentsInChildren<TMP_Text>())
        {
            tmp.ForceMeshUpdate();
        }

        // Ora rebuilda il layout dal basso verso l'alto
        foreach (var rect in targetPanel.GetComponentsInChildren<RectTransform>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        Canvas.ForceUpdateCanvases();
    }
}