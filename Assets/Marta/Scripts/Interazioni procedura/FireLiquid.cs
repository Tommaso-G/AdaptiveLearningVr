using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FireLiquid : MonoBehaviour
{
    //[SerializeField] private GameObject newFire;
    [SerializeField] private VisualEffect fireSplash;
    [SerializeField] private Transform fireParent;
    [SerializeField] private Transform spawnableFire;
    private GameObject fire;
    private float time= 3.0f;
    private int numberOfSpawn = 1;
    public bool isHit = false;
    public ErrorReporter ErrorReporter;

    private void Update()
    {
        if (isHit)
        {
            fireSplash.enabled = true;
            if (time > 0)
            {
                time -= Time.deltaTime;
            }
            else if (numberOfSpawn != 0)
            {
                SpawnFire();
                Debug.Log("Spawna");
                isHit = false;
                numberOfSpawn--;
            }
        }
        else
        {
            fireSplash.enabled = false;
        }
    }

    public void SpawnFire()
    {
        // Se non ha figli → esci
        if (spawnableFire == null || spawnableFire.childCount == 0)
            return;

        FireOnLiquidError();

        // Scegli un indice random
        int randomIndex = Random.Range(0, spawnableFire.childCount);

        // Prendi il figlio
        Transform selectedFire = spawnableFire.GetChild(randomIndex);

        if (selectedFire == null)
        {
            print("[FireLiquid] Errore nella selezione del fire da spawnare (null)");
            return;
        }
        // Tranform iniziale
        Vector3 targetScale = selectedFire.transform.localScale;
        selectedFire.localScale = Vector3.zero;
        // Cambia parent
        selectedFire.SetParent(fireParent);

        selectedFire.gameObject.GetComponent<FireObject>().enabled = false;
        selectedFire.gameObject.SetActive(true);
        StartCoroutine(GrowOverTime(selectedFire, targetScale, 5f));


    }

    private IEnumerator GrowOverTime(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = targetScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.gameObject.GetComponent<FireObject>().enabled = true;
        target.localScale = endScale; // assicurati che finisca preciso
    }

    private void FireOnLiquidError()
    {
        if (ErrorReporter != null)
        {
            ErrorReporter.RegisterError(gameObject.name);
        }
        else
        {
            Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
        }
    }
}
