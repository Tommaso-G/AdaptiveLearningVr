using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform target;  // L’oggetto da seguire

    void Update()
    {
        if (target != null)
        {
            
            transform.rotation = target.rotation; // opzionale
        }
    }
}
