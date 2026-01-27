using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.Rendering;

public class ProximitySpawner : MonoBehaviour
{     
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

    private float scaleDuration;

    

    // Per evitare spawn doppi sugli stessi oggetti
    private static readonly HashSet<Transform> spawnedTargets = new HashSet<Transform>();
    
    private RoomFire currentRoom;
    private ClosableDoor currentDoor;
    private bool hasScaled = false;
    private Vector3 initialScale;

    // cache delle scale originali dei prefab asset
    private static readonly Dictionary<GameObject, Vector3> prefabOriginalScales = new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        foreach(GameObject prefab in prefabsToSpawn)
        {
            if(prefab != null && !prefabOriginalScales.ContainsKey(prefab))
            {
                prefabOriginalScales[prefab] = prefab.transform.localScale;
            }
        }
    }

    private void Start()
    {
        if (prefabsToSpawn == null)
        {
            Debug.LogWarning($"{name}: prefabToSpawn non assegnato, lo script non farà nulla.");
            enabled = false;
            return;
        }

        scaleDuration=scaleDurationClosedDoor;


        initialScale = transform.localScale;
        StartCoroutine(ScaleThenSpawnRoutine());
        //Debug.Log($"scaleduration:{scaleDuration}");
        
    }

  
    private IEnumerator ScaleThenSpawnRoutine()
    {
        yield return new WaitUntil(() => !RiflessivoFeatures.IsPaused);
            
        float elapsed = 0f;
        hasScaled = false;

        Vector3 targetScaleVector = Vector3.one * targetScale;

        // Interpolazione lineare della scala nel tempo
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
            yield return new WaitForSeconds(Random.Range(0.1f,4f));
            Transform target = hit.transform;
            

            // Se non è già stato gestito, spawna un nuovo oggetto
            if (!spawnedTargets.Contains(target))
            {
                GameObject randomPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];
                GameObject spawned = Instantiate(randomPrefab, target.position, target.rotation);

                //Debug.Log($"{name}: Avvio dissolvenza materiale su {target.name}");

                if (prefabOriginalScales.ContainsKey(randomPrefab))
                {
                    spawned.transform.localScale = prefabOriginalScales[randomPrefab];
                    //Debug.Log($"{name}: istanziato prefab '{randomPrefab.name}' vicino a {target.name} con scala {spawned.transform.localScale} e scala originale {prefabOriginalScales[randomPrefab]}");
  
                }

                spawnedTargets.Add(target);
                StartCoroutine(BurnMaterial(target.gameObject, burnDuration));
                
                //Debug.Log($"{name}: istanziato prefab vicino a {target.name}");
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
        //Debug.Log($"collisione con: {other.name}");
        RoomFire roomFire= other.GetComponent<RoomFire>();
        if (roomFire == null)
        {
            Debug.Log("NULLO");
            return;
        }
        
        currentRoom = roomFire;
        currentDoor = roomFire.Door;  

        //Debug.Log($"{name}: entrato nella stanza {roomFire.name}, porta = {(currentDoor ? currentDoor.name : "nessuna")}, IsClosed = {(currentDoor != null ? currentDoor.IsClosed.ToString() : "n/a")}");

    if (currentDoor != null)
        {
        currentDoor.onDoorClosed.AddListener(OnDoorClosed);
        currentDoor.onDoorOpened.AddListener(OnDoorOpened);
        }

    if(currentDoor.IsClosed)  scaleDuration = scaleDurationClosedDoor;
    else scaleDuration = scaleDurationOpenedDoor;

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
        // Mostra il raggio di ricerca nell’editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private IEnumerator BurnMaterial(GameObject burnedobject, float duration)
    {
        yield return new WaitUntil(() => !RiflessivoFeatures.IsPaused);
         
        Renderer targetRenderer = burnedobject.GetComponent<Renderer>();

        if (targetRenderer == null || burnedMaterial == null)
        {
            
            yield break;
            
        }
        

        Material orginalMaterial = targetRenderer.material;
        Color originalColor = orginalMaterial.color;
        Color burnedColor = burnedMaterial.color;

        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Color blendedColor = Color.Lerp(originalColor,burnedColor, t);
            targetRenderer.material.color = blendedColor;

            yield return null;  
        }

        targetRenderer.material = burnedMaterial;
    }

    private void CheckPause()
    {

    }


}
