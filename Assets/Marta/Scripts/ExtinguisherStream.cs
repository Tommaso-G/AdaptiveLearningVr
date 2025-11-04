using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class ExtinguisherStream : MonoBehaviour
{
    [SerializeField]
    private Transform ShootingPoint;
    [SerializeField]
    private ParticleSystem FoamPS;

    private bool safetyCatch = false;
    private FireObject fire;
    private FireObject lastHit;

    //private  GameObject FoamStream;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Shooting();
    }

    public void Shooting()
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Ignore Raycast"); // ~ = simbolo per invertire => sto esculdendo il layer

        if (Physics.Raycast(ShootingPoint.position, ShootingPoint.transform.TransformDirection(Vector3.forward), out hit, 100, layerMask))
        {
            Debug.DrawRay(ShootingPoint.position, ShootingPoint.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

            fire = hit.transform.GetComponent<FireObject>();

            if (fire != lastHit)
            {
                if (lastHit != null)
                {
                    lastHit.isHit = false;
                    //Debug.Log("Primo if: " + lastHit.isHit);
                }

                lastHit = fire;
            }
            //fire hit + foam shooting
            if (fire != null && FoamPS.isPlaying)
            {
                fire.isHit = true;
                //Debug.Log("Secondo if: " + lastHit.isHit);
                fire.Extinguish();
            }
        }
        else if (fire != null) // se non sto colpendo nulla devo comunque mettere fire.isHit = false
            {
                fire.isHit = false;
                fire = null;
            }
    }

    public void StartFoam()
    {
        if (!FoamPS.isPlaying && safetyCatch)
        {   
            FoamPS.Play();
        }
    }

    public void StopFoam()
    {
        FoamPS.Clear();
        FoamPS.Pause();
    }

    public void setSafetyCatch()
    {
        Debug.Log("SC removed");
        safetyCatch = true;
    }
}
