using System.Collections;
using UnityEngine;

public class SnapOnRelease : MonoBehaviour
{
    public Transform snapTarget;
    public float snapDuration = 0.15f;
    private Coroutine coroutine;

    public void Snap(Rigidbody rb)
    {
        if (coroutine == null)
        {
           coroutine = StartCoroutine(SnapRoutine(rb));
        }
    }

    private IEnumerator SnapRoutine(Rigidbody rb)
    {
        rb.isKinematic = true;

        Transform t = rb.transform;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        float time = 0f;

        while (time < snapDuration)
        {
            float lerp = time / snapDuration;
            t.position = Vector3.Lerp(startPos, snapTarget.position, lerp);
            t.rotation = Quaternion.Slerp(startRot, snapTarget.rotation, lerp);

            time += Time.deltaTime;
            yield return null;
        }

        t.position = snapTarget.position;
        t.rotation = snapTarget.rotation;

        coroutine = null;
    }
}
