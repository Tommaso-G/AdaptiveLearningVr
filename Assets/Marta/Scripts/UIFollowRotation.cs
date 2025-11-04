using UnityEngine;

public class UIFollowRotation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Transform target;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            return;

        rectTransform.rotation = Quaternion.LookRotation(target.forward, Vector3.up);
    }
}
