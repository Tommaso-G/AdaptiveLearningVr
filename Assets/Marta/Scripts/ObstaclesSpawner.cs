using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using UnityEngine.Localization.Settings;

using System;
using System.Collections;
using Unity.XR.CoreUtils;

public class ObstaclesSpawner : MonoBehaviour
{
    [Header("Spawnables")]
    [SerializeField] private GameObject spawnablePrefab;
    public List<SpawnArea> currentSpawnAreas = new List<SpawnArea>();
    private List<SpawnArea> spawnAreas;

    [Header("Spawn Settings")]
    [SerializeField] private int maxAttemps = 100;
    [SerializeField] private float spawnRadius = 1.2f;
    [SerializeField] private float heightOfCheck = 10f;
    [SerializeField] private float rangeOfCheck = 30f;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private Transform childEmpty;
    private SpawnableObj spawnableObj;
    private bool reset = true;

    public Transform multifeedbackPos;
    private bool HasActiveSpawnedObjects => childEmpty.childCount > 0;
    private void Start()
    {
        spawnableObj = spawnablePrefab.GetComponent<SpawnableObj>();
        if (childEmpty != null)
        {
            childEmpty.gameObject.SetActive(true);
        }
    }
    public void initializeSpawn()
    {
        spawnAreas = new List<SpawnArea>(currentSpawnAreas);
        childEmpty.gameObject.SetActive(true);
        foreach (SpawnArea spawnArea in spawnAreas)
        {
            SpawnResources(spawnArea);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && HasActiveSpawnedObjects)
        {
            ResetSpawner();
        }

        if (Input.GetKeyDown(KeyCode.M) && !HasActiveSpawnedObjects)
        {
            initializeSpawn();
        }
    }

    void SpawnResources(SpawnArea spawnArea)
    {
        bool spawned = false;
        //for (int attempts = 0; attempts < maxAttemps; attempts++)
        //{
        //    Vector2 bounds = spawnArea.GetRandomPoint();
        //    RaycastHit hit;
        //    if (Physics.Raycast(new Vector3(bounds.x, heightOfCheck, bounds.y), Vector3.down, out hit, rangeOfCheck, layerMask))
        //    {
        //        if (IsSpaceFree(hit.point, spawnRadius, obstacleMask))
        //        {
        //            spawnableObj.Instantiate(spawnablePrefab, spawnArea, new Vector3(hit.point.x, hit.point.y + 0.5f, hit.point.z), Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)), childEmpty.transform);
        //            spawned = true;
        //            Debug.Log("Trovato punto random dopo " + attempts + " tentativi");
        //            break;
        //        }
        //    }
        //}

        if (!spawned)
        {
            GameObject instance = Instantiate(
                spawnablePrefab,
                new Vector3(spawnArea.SafePoint.x, spawnArea.SafePoint.y + 0.5f, spawnArea.SafePoint.z),
                Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)),
                childEmpty
            );

            SpawnableObj newSpawnable = instance.GetComponent<SpawnableObj>();

            if (newSpawnable != null)
            {
                newSpawnable.Initialize(spawnArea);
                newSpawnable.SpawnableObjDestroyed += OnSpawnedObjDestroyed;
            }
        }

        spawnArea.SetFeedbackParent(multifeedbackPos);

    }

    public void OnSpawnedObjDestroyed(SpawnableObj obj, SpawnArea spawnArea)
    {
        obj.SpawnableObjDestroyed -= OnSpawnedObjDestroyed;

        spawnArea.SetFeedbackParent(spawnArea.transform);

        currentSpawnAreas.Remove(spawnArea);
    }

    bool IsSpaceFree(Vector3 position, float radius, LayerMask mask)
    {
        return Physics.OverlapSphere(position, radius, mask).Length == 0;
    }

    public void ResetSpawner()
    {

        for (int i = childEmpty.childCount - 1; i >= 0; i--)
        {
            Destroy(childEmpty.GetChild(i).gameObject);
        }
    }
}
