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

public class ObstaclesSpawner : MonoBehaviour
{
    [Header("Spawnables")]
    [SerializeField] private GameObject spawnablePrefab;
    public List<SpawnArea> currentSpawnAreas = new List<SpawnArea>();
    private List<SpawnArea> spawnAreas;

    [SerializeField] private Transform childEmpty;
    [SerializeField] private Transform spawnAreasParent;
    private bool activateAreaEffect = false;
    private SpawnableObj spawnableObj;
    private bool Initialized = true;

    public Transform multifeedbackPos;
    private bool HasActiveSpawnedObjects => childEmpty.childCount > 0;
    private void Start()
    {
        spawnableObj = spawnablePrefab.GetComponent<SpawnableObj>();
        if (childEmpty != null)
        {
            childEmpty.gameObject.SetActive(true);
        }

        initializeSpawn();
    }
    public void initializeSpawn()
    {
        spawnAreas = new List<SpawnArea>(currentSpawnAreas);
        childEmpty.gameObject.SetActive(true);
        pickRandomArea();
        foreach (SpawnArea spawnArea in spawnAreas)
        {
            SpawnResources(spawnArea);
        }

        Initialized = true;
    }

    private void pickRandomArea(int amount = 1)
    {
        while (amount > 0)
        {
            int count = spawnAreasParent.childCount;
            SpawnArea selectedArea = null;

            if (count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, count);

                Transform pickedChild = spawnAreasParent.GetChild(randomIndex);

                selectedArea = pickedChild.GetComponent<SpawnArea>();
            }

            amount -= 1;
            if (activateAreaEffect)
            {
                selectedArea.effectActive = true;
            }

            spawnAreas.Add(selectedArea);
        }
    }

    public void ActivateAreaEffect()
    {
        activateAreaEffect = true;
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

    public void ActivateChildren()
    {

        for (int i = childEmpty.childCount - 1; i >= 0; i--)
        {
            childEmpty.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void ResetSpawner()
    {
        StartCoroutine(ResetCoroutine());
    }

    private IEnumerator ResetCoroutine()
    {
        while(!Initialized)
        {
            yield return null;
        }

        for (int i = childEmpty.childCount - 1; i >= 0; i--)
        {
            Destroy(childEmpty.GetChild(i).gameObject);
        }

        Initialized = false;
    }
}
