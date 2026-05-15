using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using UnityEngine.Localization.Settings;

using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using VRBuilder.Core.Properties;
using VRBuilder.Core.Conditions;

public class ObstaclesSpawner : MonoBehaviour, DynamicObjectInColliderCondition.IDynamicColliderProvider
{
    [Header("Spawnables")]
    [Tooltip("Tutti gli SpawnableObj già presenti in scena (disabilitati)")]
    public List<SpawnableObj> currentSceneSpawnables = new List<SpawnableObj>();
    private List<SpawnableObj> sceneSpawnables = new List<SpawnableObj>();

    public List<SpawnArea> currentSpawnAreas = new List<SpawnArea>();
    private List<SpawnArea> spawnAreas;

    [Tooltip("Tranform padre di tutti gli spawnable in scena")]
    [SerializeField] private Transform sceneSpawnablesParent;
    [Tooltip("Tranform padre di tutte le aree di spawn")]
    [SerializeField] private Transform spawnAreasParent;
    [Tooltip("Inidicare una spawn area sicura per lo spawn")]
    [SerializeField] private SpawnArea safeSpawnArea;

    private bool Initialized = true;

    public Transform multifeedbackPos;

    // Considera "attivo" se almeno uno spawnable è enabled
    private bool HasActiveSpawnedObjects => sceneSpawnables.Any(s => s.gameObject.activeSelf);

    private ColliderWithTriggerProperty _activeCollider;

    // IDynamicColliderProvider
    public ColliderWithTriggerProperty CurrentCollider => _activeCollider;

    private void Start()
    {
        for (int i = 0; i < sceneSpawnablesParent.childCount; i++)
        {
            sceneSpawnables.Add(sceneSpawnablesParent.GetChild(i).GetComponent<SpawnableObj>());
        }
        // Assicura che tutti gli spawnables partano disabilitati
        foreach (var s in sceneSpawnables)
            s.gameObject.SetActive(false);

        initializeSpawn();
    }
    public void initializeSpawn()
    {
        spawnAreas = new List<SpawnArea>(currentSpawnAreas);
        pickRandomArea();
        foreach (SpawnArea spawnArea in spawnAreas)
        {
            //SpawnResources(spawnArea);
            ActivateSpawnableForArea(spawnArea);
        }

        Initialized = true;
    }

    private void pickRandomArea(int amount = 1)
    {
        List<SpawnArea> availableAreas = new List<SpawnArea>();

        foreach (Transform child in spawnAreasParent)
        {
            SpawnArea area = child.GetComponent<SpawnArea>();
            if (area != null)
                availableAreas.Add(area);
        }

        while (amount > 0 && availableAreas.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableAreas.Count);

            SpawnArea selectedArea = availableAreas[randomIndex];

            spawnAreas.Add(selectedArea);

            availableAreas.RemoveAt(randomIndex); // 🔥 rimuove per evitare doppioni

            amount--;
        }

        // fallback se non ci sono abbastanza aree
        while (amount > 0)
        {
            spawnAreas.Add(safeSpawnArea);
            amount--;
        }
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.P) && HasActiveSpawnedObjects)
        //{
        //    ResetSpawner();
        //}

        //if (Input.GetKeyDown(KeyCode.M) && !HasActiveSpawnedObjects)
        //{
        //    initializeSpawn();
        //}
    }

    private void ActivateSpawnableForArea(SpawnArea spawnArea)
    {
        if (spawnArea == null)
        {
            if (safeSpawnArea != null) spawnArea = safeSpawnArea;
            else { print("[ObstaclesSpawner] spawnArea null"); return; }
        }

        // 1️⃣ Trova tutti gli spawnable validi per quell'area e inattivi
        var candidates = sceneSpawnables
            .Where(s => s.AssignedSpawnArea == spawnArea && !s.gameObject.activeSelf)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[ObstaclesSpawner] Nessun SpawnableObj disponibile per l'area '{spawnArea.name}'.");
            return;
        }

        // 2️⃣ Selezione random
        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        SpawnableObj target = candidates[randomIndex];

        _activeCollider = target.GetComponentInChildren<ColliderWithTriggerProperty>(true);
        target.SpawnableObjDestroyed += OnSpawnedObjDestroyed;
        target.Activate();

        target.SetFeedbackParent(multifeedbackPos);

        currentSceneSpawnables.Add(target);
    }

    public void OnSpawnedObjDestroyed(SpawnableObj obj, SpawnArea spawnArea)
    {
        obj.SpawnableObjDestroyed -= OnSpawnedObjDestroyed;
        //Destroy(obj.gameObject);
        _activeCollider = null;
        currentSpawnAreas.Remove(spawnArea);
        currentSceneSpawnables.Remove(obj);
    }

    public void ActivateChildren()
    {
        foreach (var s in currentSceneSpawnables)
            s.gameObject.SetActive(true);
    }
    public void ResetSpawner()
    {
        StartCoroutine(ResetCoroutine());
    }

    private IEnumerator ResetCoroutine()
    {
        while (!Initialized)
            yield return null;

        foreach (var spawnable in sceneSpawnables.Where(s => s.gameObject.activeSelf))
        {
            spawnable.PrepareForDestroy();
        }

        Initialized = false;
    }
}
