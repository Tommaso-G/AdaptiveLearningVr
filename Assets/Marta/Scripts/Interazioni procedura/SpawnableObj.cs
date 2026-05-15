using System;
using UnityEditor;

using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SpawnableObj : MonoBehaviour
{
    [SerializeField] private SpawnArea spawnArea;
    [SerializeField] private Transform feedbackPos;
    public SpawnArea AssignedSpawnArea => spawnArea;

    private IDestructible destructible;

    public event Action<SpawnableObj, SpawnArea> SpawnableObjDestroyed;

    public void Activate()
    {
        if (spawnArea != null)
        {
            spawnArea.Initialize();
            //spawnArea.SetOccupied(true);
        }

        destructible = GetComponent<IDestructible>();
        if (destructible != null)
        {
            destructible.OnDestroyed += HandleDestroyed;
        }
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

    public void SetFeedbackParent(Transform newParent)
    {
        if (feedbackPos != null)
            feedbackPos.parent = newParent;
        print($"[SpawnArea] Posizione feedback {feedbackPos.gameObject.name}");
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
            SpawnableObjDestroyed?.Invoke(this, spawnArea);
        }

        SetFeedbackParent(transform);
        Deactivate();
    }

    public void Deactivate()
    {
        if (destructible != null)
        {
            destructible.OnDestroyed -= HandleDestroyed;
            destructible = null;
        }

        gameObject.SetActive(false);
    }
}
