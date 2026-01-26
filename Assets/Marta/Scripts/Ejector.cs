using UnityEngine;

public class Ejector : MonoBehaviour
{
    [SerializeField]
    private Transform ejectorOrigin;
    [SerializeField]
    private GameObject dinamicRope;
    [SerializeField]
    private GameObject staticRope;

    private void Start()
    {
        backToOrigin();

    }
    public void backToOrigin()
    {
        transform.position = ejectorOrigin.position;
        transform.rotation = ejectorOrigin.rotation;
        //staticRope.SetActive(true);
        //dinamicRope.SetActive(false);
    }

    public void isGrabbed()
    {
        //staticRope.SetActive(false);
        //dinamicRope.SetActive(true);

    }
}
