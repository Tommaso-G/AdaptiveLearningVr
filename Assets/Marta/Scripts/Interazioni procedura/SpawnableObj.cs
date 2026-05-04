using System;
using UnityEditor;

using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SpawnableObj : MonoBehaviour
{
    [SerializeField] private SpawnArea spawnArea;

    public event Action<SpawnableObj, SpawnArea> SpawnableObjDestroyed;

    private bool _isQuitting;

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    // Start is called once before the first executon of Update after the MonoBehaviour is created
    public void Initialize(SpawnArea area)
    {
        spawnArea = area;
        //spawnArea.SetOccupied(true);
    }

    private void Start()
    {
        spawnArea.SetOccupied(true);
    }

    private void OnDestroy()
    {

        if (_isQuitting)
            return;
#if UNITY_EDITOR
        // Questo serve a distinguere distruzione in-game da chiusura Play Mode
        if (!Application.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif

        // Controlla sempre che l'oggetto non sia distrutto
        if (spawnArea != null && spawnArea.gameObject != null)
        {
            spawnArea.SetOccupied(false);
            SpawnableObjDestroyed?.Invoke(this, spawnArea);
        }
    }
}
