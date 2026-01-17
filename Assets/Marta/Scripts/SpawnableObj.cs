using System;
using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SpawnableObj : MonoBehaviour
{
    [SerializeField] private SpawnArea spawnArea;
    // Start is called once before the first executon of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void Instantiate(GameObject prefab, SpawnArea spawnArea, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null || spawnArea == null)
        {
            Debug.LogWarning("SpawnableObject: prefab o area null");
            return;
        }
        this.spawnArea = spawnArea;
        Instantiate(prefab, position, rotation, parent);
        spawnArea.SetOccupied(true);
    }

    private void OnDestroy()
    {
        spawnArea?.SetOccupied(false);
    }
}
