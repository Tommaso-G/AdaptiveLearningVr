using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetObjectsOnCollision : MonoBehaviour
{
    [Header("Oggetti da controllare")]
    public List<GameObject> objectsToTrack;

    [Header("Collider che attiva il reset")]
    public Collider targetCollider;

    [Header("Velocità ritorno")]
    public float returnSpeed = 2f;

    // Posizioni e rotazioni iniziali
    private Dictionary<GameObject, Vector3> startPositions =
        new Dictionary<GameObject, Vector3>();

    private Dictionary<GameObject, Quaternion> startRotations =
        new Dictionary<GameObject, Quaternion>();

    // Evita coroutine multiple
    private HashSet<GameObject> movingObjects =
        new HashSet<GameObject>();

    void Start()
    {
        foreach (GameObject obj in objectsToTrack)
        {
            if (obj != null)
            {
                // Salva posizione iniziale
                startPositions[obj] = obj.transform.position;

                // Salva rotazione iniziale
                startRotations[obj] = obj.transform.rotation;

                // Aggiunge il gestore collisioni
                ObjectCollisionHandler handler =
                    obj.AddComponent<ObjectCollisionHandler>();

                handler.manager = this;
            }
        }
    }

    public void CheckCollision(GameObject obj, Collider other)
    {
        // Controlla che il collider sia quello corretto
        if (other == targetCollider)
        {
            ResetObject(obj);
        }
    }

    private void ResetObject(GameObject obj)
    {
        if (!movingObjects.Contains(obj))
        {
            StartCoroutine(MoveBack(obj));
        }
    }

    private IEnumerator MoveBack(GameObject obj)
    {
        movingObjects.Add(obj);

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        // Disattiva la fisica durante il movimento
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 initialPos = obj.transform.position;
        Quaternion initialRot = obj.transform.rotation;

        Vector3 targetPos = startPositions[obj];
        Quaternion targetRot = startRotations[obj];

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;

            // Movimento
            obj.transform.position =
                Vector3.Lerp(initialPos, targetPos, t);

            // Rotazione
            obj.transform.rotation =
                Quaternion.Slerp(initialRot, targetRot, t);

            yield return null;
        }

        // Assicura precisione finale
        obj.transform.position = targetPos;
        obj.transform.rotation = targetRot;

        // Riattiva la fisica
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        movingObjects.Remove(obj);
    }
}

public class ObjectCollisionHandler : MonoBehaviour
{
    [HideInInspector]
    public ResetObjectsOnCollision manager;

    private void OnTriggerEnter(Collider other)
    {
        manager.CheckCollision(gameObject, other);
    }

    /*
    // Usa questo se NON usi trigger
    private void OnCollisionEnter(Collision collision)
    {
        manager.CheckCollision(gameObject, collision.collider);
    }
    */
}