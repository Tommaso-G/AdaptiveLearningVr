using System;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    [Header("Spawn Area Settings")]
    [SerializeField] private Transform safeSpawnPoint;

    [Header("Effects")]
    [SerializeField] private FullScreenEffectsController effectsController;
    [SerializeField] private GameObject areaPSEffect;
    public bool effectActive;

    private Bounds areaBounds;
    private bool isOccupied;
    public bool IsOccupied => isOccupied;
    public Vector3 SafePoint
    {
        get
        {
            if (safeSpawnPoint == null)
            {
                Debug.LogError("SafePoint è stato distrutto o non assegnato!");
                return Vector3.zero;
            }

            return safeSpawnPoint.position;
        }
    }

    public static event Action<SpawnArea, bool> onSpawnAreaChange;

    public Transform feedbackPos;

    public Bounds AreaBounds => areaBounds;
    private void Awake()
    {
        CacheareaBounds();
    }
    private void CacheareaBounds()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
        {
            Debug.LogError($"SpawnArea '{name}' has no mesh.");
            return;
        }

        Vector3 center = transform.TransformPoint(mesh.bounds.center);
        Vector3 size = Vector3.Scale(mesh.bounds.size, transform.lossyScale);
        areaBounds = new Bounds(center, size);
    }


    public Vector2 GetRandomPoint()
    {
        float x = UnityEngine.Random.Range(areaBounds.min.x, areaBounds.max.x);
        float z = UnityEngine.Random.Range(areaBounds.min.z, areaBounds.max.z);
        return new Vector3(x, z);
    }

    public void SetOccupied(bool value)
    {
        isOccupied = value;
        onSpawnAreaChange?.Invoke(this, value);
        print("OnAreaChange");
        if (value && effectActive)
        {
            SpawnEffect();
        }
        else
        {
            ResetArea();
        }
    }

    public void SpawnEffect()
    {
        Collider volume = GetComponent<Collider>();
        if (areaPSEffect == null || volume == null)
        {
            Debug.LogWarning("SpawnableObject: prefab o volume null");
            return;
        }

        ParticleSystem ps = areaPSEffect.GetComponentInChildren<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning("SpawnableObject: prefab non contiene ParticleSystem");
            return;
        }

        // Configura forma Box
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volume.bounds.size;
        shape.position = volume.bounds.center;

        // Istanzia e fa partire il particle system
        GameObject instance = Instantiate(areaPSEffect, Vector3.zero, Quaternion.identity, this.transform);
        instance.GetComponent<ParticleSystem>().Play();

        if (effectsController != null)
        {
            effectsController.SetCanActivate(true);
        }
    }

    public void SetFeedbackParent(Transform newParent)
    {
        if (feedbackPos != null)
            feedbackPos.parent = newParent;
    }

    public void ResetArea()
    {
        isOccupied = false;

        for (int i = transform.childCount - 1; i > 0; i--)
        {
            if (transform.GetChild(i).GetComponent<ParticleSystem>() != null)
            {
                Destroy(transform.GetChild(i).gameObject);
                break;
            }
        }

        if (effectsController != null)
        {
            effectsController.SetCanActivate(false);
        }
    }
}
