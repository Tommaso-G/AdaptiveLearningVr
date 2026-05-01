using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.VFX;

public class ExtinguisherStream : MonoBehaviour
{
    public Transform ShootingPoint;
    public ParticleSystem FoamPS;

    private bool safetyCatch = false;
    private FireObject fire;
    private FireObject lastHit;
    private FireLiquid fireLiquid;
    private FireLiquid lastFireLiquid;
    public ExecutionOrderController executionOrderController;
    public GameObject objectToFlash;

    //private  GameObject FoamStream;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        executionOrderController = FindAnyObjectByType<ExecutionOrderController>();
    }

    // Update is called once per frame
    void Update()
    {
        Shooting();
        fireSplashRayCast();
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

    private void fireSplashRayCast()
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Fire Liquid");

        if (Physics.Raycast(ShootingPoint.position, ShootingPoint.transform.TransformDirection(Vector3.forward), out hit, 100, layerMask))
        {
            Debug.DrawRay(ShootingPoint.position, ShootingPoint.transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);

            fireLiquid = hit.transform.GetComponent<FireLiquid>();

            if (fireLiquid != lastFireLiquid)
            {
                if (lastFireLiquid != null)
                {
                    lastFireLiquid.isHit = false;
                }

                lastFireLiquid = fireLiquid;
            }
            //fire hit + foam shooting
            if (fireLiquid != null && FoamPS.isPlaying)
            {
                fireLiquid.isHit = true;
            }
        }
        else if (fireLiquid != null) // se non sto colpendo nulla devo comunque mettere fire.isHit = false
        {
            fireLiquid.isHit = false;
            //fLiquid = false;
            fireLiquid = null;
        }


    }

    public void StartFoam()
    {
        Debug.Log("Chiamato start foam");
        if (!safetyCatch)
        {
            string chapterName = ErrorEvent.process.Data.Current.Data.Name;
            string stepName = ErrorEvent.process.Data.Current.Data.Current.Data.Name;
            ErrorEvent.OnError.Invoke("Estintore generico Optional", stepName, transform.name);

            if (executionOrderController != null && objectToFlash != null)
                executionOrderController.DifferentStepWarningHighlight(objectToFlash);
        }
        else if(!FoamPS.isPlaying && safetyCatch)
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
        safetyCatch = true;
    }
}
