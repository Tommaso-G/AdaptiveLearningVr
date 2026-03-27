using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.Rendering;

public class ProximitySpawner : MonoBehaviour
{     
    public ExecutionOrderController executionOrderController;
    public StepErrorTracker errorTracker;
    public GameObject objectToFlash;



    [Tooltip("Raggio entro cui cercare oggetti del layer target.")]
    public float detectionRadius = 5f;

    public LayerMask targetLayer;

    [Tooltip("Prefab da instanziare presso i target trovati.")]
    public List<GameObject> prefabsToSpawn;

    public Material burnedMaterial;

    [Tooltip("Scala finale da raggiungere prima dello spawn.")]
    public float targetScale = 2f;

    [Tooltip("Durata della scalatura (in secondi).")]
    public float scaleDurationOpenedDoor = 2f;

    [Tooltip("Durata della scalatura (in secondi).")]
    public float scaleDurationClosedDoor = 2f;

    public float burnDuration = 6f;

    [Tooltip("Scala iniziale forzata per tutti gli oggetti spawnati.")]
    public float spawnInitialScale = 0.04f;

    private float scaleDuration;

    private static readonly HashSet<Transform> spawnedTargets = new HashSet<Transform>();
    
    private RoomFire currentRoom;
    private ClosableDoor currentDoor;
    private bool hasScaled = false;
    private Vector3 initialScale;

    private void Start()
    {
        Debug.Log($"{name}: Start called. localScale = {transform.localScale}, position = {transform.position}");

        if (prefabsToSpawn == null)
        {
            Debug.LogWarning($"{name}: prefabToSpawn non assegnato.");
            enabled = false;
            return;
        }

        scaleDuration = scaleDurationClosedDoor;

        // Forza sempre la scala iniziale a un valore noto e pulito
        initialScale = Vector3.one * spawnInitialScale;
        transform.localScale = initialScale;

        StartCoroutine(ScaleThenSpawnRoutine());
    }

    private IEnumerator ScaleThenSpawnRoutine()
    {
        yield return new WaitUntil(() => !RiflessivoFeatures.IsPaused);
            
        float elapsed = 0f;
        hasScaled = false;

        Vector3 targetScaleVector = Vector3.one * targetScale;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDuration);
            transform.localScale = Vector3.Lerp(initialScale, targetScaleVector, t);
            yield return null;
        }

        hasScaled = true;
        transform.localScale = targetScaleVector;

        StartCoroutine(CheckNearbyObjects());
    }

    private IEnumerator CheckNearbyObjects()
    {
        yield return new WaitUntil(() => !RiflessivoFeatures.IsPaused);

        if (!hasScaled)
            yield break;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);

        foreach (Collider hit in hits)
        {   
            yield return new WaitForSeconds(Random.Range(0.1f, 4f));
            Transform target = hit.transform;

            if (!spawnedTargets.Contains(target))
            {
                GameObject randomPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];
                GameObject spawned = Instantiate(randomPrefab, target.position, target.rotation);

                // Scala iniziale sempre forzata a un valore noto, ignorando
                // qualsiasi scala sporca ereditata dal prefab asset
                spawned.transform.localScale = Vector3.one * spawnInitialScale;
                Debug.Log($"Spawned {spawned.name}, scala dopo assign = {spawned.transform.localScale}");

                spawnedTargets.Add(target);
                StartCoroutine(BurnMaterial(target.gameObject, burnDuration));
            }
        }

        if (currentDoor != null)
        {
            currentDoor.onDoorClosed.RemoveListener(OnDoorClosed);
            currentDoor.onDoorOpened.RemoveListener(OnDoorOpened);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //CASO 1: Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entrato nel trigger");

            if (errorTracker == null)
            {
                Debug.LogWarning($"{name}: errorTracker non assegnato.");
                return;
            }

            string chapterName = errorTracker.CurrentProcess?.Data.Current?.Data.Name ?? "Unknown Chapter";
            string stepName = errorTracker.CurrentProcess?.Data.Current?.Data.Current?.Data.Name ?? "Unknown Step";

            errorTracker.RegisterError(chapterName, stepName, "Fuoco");

            if (executionOrderController != null && objectToFlash != null)
                executionOrderController.DifferentStepWarningHighlight(objectToFlash);

            return; 
        }

        // CASO 2: RoomFire
        RoomFire roomFire = other.GetComponent<RoomFire>();
        if (roomFire == null)
        {
            Debug.Log("NULLO");
            return;
        }

        currentRoom = roomFire;
        currentDoor = roomFire.Door;

        if (currentDoor != null)
        {
            currentDoor.onDoorClosed.AddListener(OnDoorClosed);
            currentDoor.onDoorOpened.AddListener(OnDoorOpened);

            if (currentDoor.IsClosed)
                scaleDuration = scaleDurationClosedDoor;
            else
                scaleDuration = scaleDurationOpenedDoor;
        }
    }
    private void OnDoorClosed()
    {
        scaleDuration = scaleDurationClosedDoor;
    }

    private void OnDoorOpened()
    {
        scaleDuration = scaleDurationOpenedDoor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private IEnumerator BurnMaterial(GameObject burnedobject, float duration)
    {
        yield return new WaitUntil(() => !RiflessivoFeatures.IsPaused);
         
        Renderer targetRenderer = burnedobject.GetComponent<Renderer>();

        if (targetRenderer == null || burnedMaterial == null)
            yield break;

        Material orginalMaterial = targetRenderer.material;
        Color originalColor = orginalMaterial.color;
        Color burnedColor = burnedMaterial.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Color blendedColor = Color.Lerp(originalColor, burnedColor, t);
            targetRenderer.material.color = blendedColor;

            yield return null;  
        }

        targetRenderer.material = burnedMaterial;
    }


}