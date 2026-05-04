using UnityEngine;

public class UIPositionController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Transform lookAtTarget;
    private RectTransform rectTransform;

    void Start()
    {
        lookAtTarget = Camera.main.transform;
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (lookAtTarget == null)
            return;

        rectTransform.rotation = Quaternion.LookRotation(lookAtTarget.forward, Vector3.up);

    }

    public void changePos(Transform targetPos)
    {
        float yPos = targetPos.position.y + (rectTransform.rect.height * rectTransform.localScale.y) / 2;
        rectTransform.position = new  Vector3(targetPos.position.x, yPos, targetPos.position.z);
    }
}
