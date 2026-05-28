using System;
using System.Collections;
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

    public Transform effectFeedbackPos;
    public Transform checkFumo;

    public Bounds AreaBounds => areaBounds;
    private void Awake()
    {
        CacheareaBounds();
    }

    public void Initialize()
    {
        checkFumo.position = effectFeedbackPos.position;
        checkFumo.rotation = effectFeedbackPos.rotation;
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
        if (!value)
        {
            ResetArea();
        }
    }

    public void SpawnEffect()
    {
        if (!effectActive) return;

        checkFumo.gameObject.SetActive(false);
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
    public void ResetArea()
    {
        isOccupied = false;

        for (int i = transform.childCount - 1; i > 0; i--)
        {
            ParticleSystem ps = transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps!= null)
            {
                StartCoroutine(EffectOff(ps));
                break;
            }
        }

        if (effectsController != null)
        {
            effectsController.SetCanActivate(false);
        }
    }

    private IEnumerator EffectOff(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        while (ps.IsAlive(true))
        {
            yield return null;
        }

        Destroy(ps.gameObject);
    }
}
