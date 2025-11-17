using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FireLiquid : MonoBehaviour
{
    [SerializeField] private GameObject newFire;
    [SerializeField] private VisualEffect fireSplash;
    private GameObject fire;
    private float time= 5.0f;
    private int numberOfSpawn = 1;
    public bool isHit = false;

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
        Vector3 randomOffset;
        do
        {
            randomOffset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        } while (Mathf.Abs(randomOffset.x) < 0.2f && Mathf.Abs(randomOffset.z) < 0.2f);


        Vector3 spawnPosition = transform.position + randomOffset;

        fire = Instantiate(newFire, spawnPosition, Quaternion.identity);
        fire.GetComponent<FireObject>().enabled = false;
        fire.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
        StartCoroutine(GrowOverTime(fire.transform, 3f));

    }

    private IEnumerator GrowOverTime(Transform target, float duration)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = new Vector3(0.2f, 0.2f, 0.2f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fire.GetComponent<FireObject>().enabled = true;
        target.localScale = endScale; // assicurati che finisca preciso
    }

}
