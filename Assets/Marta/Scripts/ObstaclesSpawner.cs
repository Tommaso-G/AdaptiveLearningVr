using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using UnityEngine.Localization.Settings;

using System;

public class ObstaclesSpawner : MonoBehaviour
{
    [Header("Spawn settings")]
    public GameObject objToSpawn;
    public GameObject smokeToSpawn;
    public List<GameObject> spawnPlaneGrid;
    public float spawnChance;
    public float spawnRadius = 1.2f;
    public int maxAttemps = 100;
    public Transform safeSpawnPoints;
    public bool smoke;

    [Header("Raycast setup")]
    public float distanceBetweenCheck;
    public float heightOfCheck = 10f, rangeOfCheck = 30f;
    public LayerMask layerMask;
    public LayerMask obstacleMask;
    public Vector2 positivePosition, negativePosition;

    private Transform childEmpty;
    private SpawnableObj spawnableObj;
    private void Start()
    {
        smoke = false;
        spawnableObj = objToSpawn.GetComponent<SpawnableObj>();
        SpawnableObj.onSpawnAreaChange += StopSmoke;
        childEmpty = GetComponentsInChildren<Transform>().FirstOrDefault(t => t != transform);
        if (childEmpty != null)
        {
            childEmpty.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("childEmpty non trovato");
        }
    }
    private void initializeSpawn()
    {
        childEmpty.gameObject.SetActive(true);
        foreach (GameObject spawnPlane in spawnPlaneGrid)
        {
            safeSpawnPoints = spawnPlane.GetComponentsInChildren<Transform>().FirstOrDefault(t => t != spawnPlane.transform);
            getSpawnAreaLimits(spawnPlane);
            SpawnResources(spawnPlane, smoke);
        }
    }

    private void getSpawnAreaLimits(GameObject spawnPlane)
    {
        Mesh mesh = spawnPlane.GetComponent<MeshFilter>().mesh;
        Vector3[] localVertices = mesh.vertices;

        Vector3 worldPos = spawnPlane.transform.TransformPoint(localVertices[0]);
        Vector2 Upperlimit = new Vector2 (worldPos.x, worldPos.z);
        Vector2 Lowerlimit = new Vector2(worldPos.x, worldPos.z);

        for (int i = 1; i < localVertices.Length; i++)
        {
            worldPos = spawnPlane.transform.TransformPoint(localVertices[i]);

            Upperlimit.x = Mathf.Max(Upperlimit.x, worldPos.x);
            Upperlimit.y = Mathf.Max(Upperlimit.y, worldPos.z);

            Lowerlimit.x = Mathf.Min(Lowerlimit.x, worldPos.x);
            Lowerlimit.y = Mathf.Min(Lowerlimit.y, worldPos.z);
        }

        positivePosition.x = Upperlimit.x;
        positivePosition.y = Upperlimit.y;

        negativePosition.x = Lowerlimit.x;
        negativePosition.y = Lowerlimit.y;

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DeleteResources();
            initializeSpawn();
        }

        if(childEmpty?.childCount == 0)
        {
            childEmpty.gameObject.SetActive(false);
        }
    }

    void SpawnResources(GameObject spawnPlane, bool smoke = false)
    {
        bool spawned = false;
        for (int attempts = 0; attempts < maxAttemps; attempts++)
        {
            float x = UnityEngine.Random.Range(negativePosition.x, positivePosition.x);
            float z = UnityEngine.Random.Range(negativePosition.y, positivePosition.y);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(x, heightOfCheck, z), Vector3.down, out hit, rangeOfCheck, layerMask))
            {
                if (IsSpaceFree(hit.point, spawnRadius, obstacleMask))
                {
                    spawnableObj.Instantiate(objToSpawn, spawnPlane, new Vector3(hit.point.x, hit.point.y + 0.5f, hit.point.z), Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)), childEmpty.transform);
                    spawned = true;
                    Debug.Log("Trovato punto random dopo " + attempts + " tentativi");
                    break;
                }
            }
        }

        if (!spawned)
        {
            spawnableObj.Instantiate(objToSpawn, spawnPlane, new Vector3(safeSpawnPoints.position.x, safeSpawnPoints.position.y + 0.5f, safeSpawnPoints.position.z), Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)), childEmpty.transform);
        }

        if (smoke)
        {
            spawnableObj.Instantiate(smokeToSpawn, spawnPlane,Vector3.zero, Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)), childEmpty.transform, spawnPlane.GetComponent<BoxCollider>());
            //spawnPlane.GetComponentInChildren<ParticleSystem>()?.Play();
        }
    }

    bool IsSpaceFree(Vector3 position, float radius, LayerMask mask)
    {
        return Physics.OverlapSphere(position, radius, mask).Length == 0;
    }

    private void StopSmoke(GameObject spawnArea, bool occupied)
    {
        if (!occupied)
        {
            spawnArea.GetComponentInChildren<ParticleSystem>()?.Stop();
        }
    }

    void DeleteResources()
    {
        for (int i = childEmpty.childCount - 1; i >= 0; i--)
        {
            Destroy(childEmpty.GetChild(i).gameObject);
        }
    } 
}
