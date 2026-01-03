using System;
using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SpawnableObj : MonoBehaviour
{
    public GameObject spawnArea;
    public BoxCollider spawnVolume;
    public static event Action<GameObject, bool> onSpawnAreaChange;
    // Start is called once before the first executon of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void Instantiate(GameObject prefab, GameObject spawnArea, Vector3 position, Quaternion rotation, Transform parent, Collider spawnVolume = null)
    {
        if (spawnVolume != null)
        {
            ParticleSystem.ShapeModule boxShape = prefab.GetComponentInChildren<ParticleSystem>().shape;
            boxShape.shapeType = ParticleSystemShapeType.Box;
            boxShape.scale = spawnVolume.bounds.size;
            boxShape.position = spawnVolume.bounds.center;
            GameObject smoke = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            smoke.GetComponent<ParticleSystem>().Play();
            return;
        }

        this.spawnArea = spawnArea;
        Instantiate(prefab, position, rotation, parent);
        onSpawnAreaChange?.Invoke(spawnArea, true);

    }

    private void OnDestroy()
    {
        onSpawnAreaChange?.Invoke(spawnArea, false);
    }
}
