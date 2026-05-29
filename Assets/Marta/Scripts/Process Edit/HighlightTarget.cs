using UnityEngine;

public class HighlightTarget : MonoBehaviour
{
    [SerializeField] GameObject target;

    public GameObject GetTargetToHighlight()
    {
        if(target == null)
        {
            Debug.Log($"[HighlightTarget] Oggetto Target null.");
        }

        return target;
    }
}
