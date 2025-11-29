using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera targetCamera; // opzionale: se lasci vuoto, userà Camera.main
    public bool onlyYAxis = false; // se true, ruota solo attorno all'asse Y

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (onlyYAxis)
        {
            // Calcola la direzione verso la camera, ma solo sull'asse Y
            Vector3 direction = targetCamera.transform.position - transform.position;
            direction.y = 0f; // blocca rotazione verticale
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-direction);
        }
        else
        {
            // Guarda sempre verso la camera
            transform.LookAt(
                transform.position + targetCamera.transform.rotation * Vector3.forward,
                targetCamera.transform.rotation * Vector3.up
            );
        }
    }
}
