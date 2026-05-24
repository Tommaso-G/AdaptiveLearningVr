using UnityEngine;

public class SetWorldYByName : MonoBehaviour
{
    public string[] targetNames;
    public float targetWorldY = 0f;

    void Awake()
    {
        if (targetNames == null || targetNames.Length == 0) return;

        var names = new System.Collections.Generic.HashSet<string>(targetNames);

        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!names.Contains(obj.name)) continue;

            Vector3 pos = obj.transform.position;
            pos.y = targetWorldY;
            obj.transform.position = pos;
        }
    }
}