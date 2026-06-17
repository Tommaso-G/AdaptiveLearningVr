using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerSample
{
    public float timestamp;
    public float x;
    public float z; // piano XZ
}

[Serializable]
public class PlayerPathData
{
    public List<PlayerSample> samples = new List<PlayerSample>();
}

public class PlayerPathSampler : MonoBehaviour
{
    [Header("Riferimenti")]
    public Transform playerTransform;
    public Transform origin; // Empty che funge da (0,0); se null usa world-space

    [Header("Impostazioni")]
    public float sampleInterval = 1f; // secondi tra un campione e l'altro

    [Header("Salvataggio")]
    public string fileName = "player_path.json";

    private PlayerPathData _data = new PlayerPathData();
    private float _elapsed = 0f;

    private void Update()
    {
        if (playerTransform == null) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= sampleInterval)
        {
            _elapsed = 0f;
            CampionaPlayer();
        }
    }

    private void CampionaPlayer()
    {
        Vector3 pos = playerTransform.position;

        if (origin != null)
            pos = origin.InverseTransformPoint(pos); // coordinate locali rispetto all'empty

        var sample = new PlayerSample
        {
            timestamp = Time.time,
            x = pos.x,
            z = pos.z
        };

        _data.samples.Add(sample);
    }


    public void SalvaDati(string directory, string nomeFile)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, nomeFile);
        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(path, json);
        Debug.Log($"[PlayerPathSampler] Salvato in: {path}");
        
        _data.samples.Clear(); // reset per iterazione successiva
    }
}