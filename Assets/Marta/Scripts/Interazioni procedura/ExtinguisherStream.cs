using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.VFX;
using System.Collections.Generic;

public class ExtinguisherStream : MonoBehaviour
{
    public Transform ShootingPoint;
    public ParticleSystem FoamPS;
    public ErrorReporter ErrorReporter;

    [SerializeField] private List<FireType> supportedTypes;

    [SerializeField] private GameObject safetyCatchEmpty;
    private bool safetyCatch = false;
    private FireObject fire;
    private FireObject lastHit;
    private FireLiquid fireLiquid;
    private FireLiquid lastFireLiquid;

    private bool wrongType = false;

    //private  GameObject FoamStream;

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
                if (supportedTypes.Contains(fire.FireType))
                {
                    fire.isHit = true;
                    fire.Extinguish();
                }
                else
                {
                    fire.isHit = false;

                    if (ErrorReporter != null && !wrongType)
                    {
                        ErrorReporter.RegisterError("Wrong extinguisher");
                        wrongType = true;
                    }
                }
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
            if(ErrorReporter != null)
            {
                ErrorReporter.RegisterError(gameObject.name);
            }
            else
            {
                Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
            }
        }
        else if(!FoamPS.isPlaying && safetyCatch)
        {
            FoamPS.Play();
        }
    }

    public void StopFoam()
    {
        wrongType = false;
        FoamPS.Clear();
        FoamPS.Pause();
    }

    public void setSafetyCatch()
    {
        safetyCatch = true;
        SafetyCatchCheck(true);
    }

    public void SafetyCatchCheck(bool disable)
    {
        if (safetyCatch)
        {
            if (disable)
            {
                safetyCatchEmpty.SetActive(false);
            }
            else
            {
                safetyCatchEmpty.SetActive(true);
            }
        }
    }
}
