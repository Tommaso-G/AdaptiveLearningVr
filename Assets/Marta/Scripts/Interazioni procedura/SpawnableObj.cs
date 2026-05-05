using System;
using UnityEditor;

using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SpawnableObj : MonoBehaviour
{
    [SerializeField] private SpawnArea spawnArea;

    private IDestructible destructible;

    public event Action<SpawnableObj, SpawnArea> SpawnableObjDestroyed;
    // Start is called once before the first executon of Update after the MonoBehaviour is created
    public void Initialize(SpawnArea area)
    {
        spawnArea = area;
        spawnArea.Initialize();
        //spawnArea.SetOccupied(true);
    }

    private void Start()
    {
        print("[SpanwnableObj] Chimato Start");
        spawnArea.SetOccupied(true);
        destructible = GetComponent<IDestructible>();

        if (destructible != null)
        {
            destructible.OnDestroyed += HandleDestroyed;
        }
    }

    private void HandleDestroyed()
    {
        PrepareForDestroy();
    }

    public void PrepareForDestroy()
    {
        if (spawnArea != null && spawnArea.gameObject != null)
        {
            spawnArea.SetOccupied(false);
            spawnArea.SetFeedbackParent(spawnArea.transform);
            SpawnableObjDestroyed?.Invoke(this, spawnArea);
        }
    }
}
