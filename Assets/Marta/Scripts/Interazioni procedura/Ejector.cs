using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
public class Ejector : MonoBehaviour
{
    [SerializeField]
    private Transform ejectorOrigin;
    [SerializeField]
    private GameObject dynamicRope;
    [SerializeField]
    private GameObject staticRope;

    private float duration = 1f;

    private void Start()
    {
        MoveTo(ejectorOrigin, duration);
    }

    public void MoveTo(Transform target, float duration)
    {
        StartCoroutine(LerpPosition(target, duration));
    }

    IEnumerator LerpPosition(Transform target, float duration)
    {
        Vector3 posStart = transform.position;
        Quaternion rotStart = transform.rotation;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            transform.position = Vector3.Lerp(posStart, target.position, t);
            transform.rotation = Quaternion.Lerp(rotStart, target.rotation, t);


            yield return null;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
    public void backToOrigin()
    {
        transform.position = ejectorOrigin.position;
        transform.rotation = ejectorOrigin.rotation;
        //staticRope.SetActive(true);
        //foreach (MeshRenderer r in DynamicMeshes)
        //{
        //    r.enabled = false;
        //}
    }

    public void isGrabbed()
    {
        //staticRope.SetActive(false);
        //foreach (MeshRenderer r in DynamicMeshes)
        //{
        //    r.enabled = true;
        //}

    }
}
