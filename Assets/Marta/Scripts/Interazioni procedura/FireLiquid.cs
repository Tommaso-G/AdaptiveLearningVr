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
    [SerializeField] private ParticleSystem fireParticles;
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
        // Cambia parent
        selectedFire.SetParent(fireParent, true);

        //selectedFire.gameObject.GetComponent<FireObject>().enabled = false;
        selectedFire.gameObject.SetActive(true);
        //StartCoroutine(GrowOverTime(selectedFire));


    }

    private IEnumerator GrowOverTime(Transform selectedFire)
    {
        Animator anim = selectedFire.GetComponentInChildren<Animator>();
        if(anim != null)
        {
           anim.SetTrigger("grow");
        }

        yield return new WaitForSeconds(3f);

        fireParticles?.Play();
        anim.gameObject.SetActive(false);


        selectedFire.gameObject.GetComponent<FireObject>().enabled = true;
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
