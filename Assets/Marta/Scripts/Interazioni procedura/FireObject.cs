    using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
public class FireObject : MonoBehaviour
{
    [SerializeField] private Transform fireTran;
    [SerializeField] private BoxCollider fireBox;
    [SerializeField] private SphereCollider damageArea;
    [SerializeField] private ParticleSystem firePS;
    [SerializeField] private float fireHealt = 1f;
    [SerializeField] private float maxHealt = 1f;
    [SerializeField] private float fireDamage;
    private Vector3 initialScale;
    private bool sizeChanging = false;
    private bool isExtinguishing = false;
    public bool isHit = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform parentTR = GetComponent<Transform>();
        Transform[] tranforms = GetComponentsInChildren<Transform>();
        fireTran = tranforms.FirstOrDefault(t => t != parentTR);
        initialScale = fireTran.localScale;
        fireBox = GetComponent<BoxCollider>();
        damageArea = GetComponentInChildren<SphereCollider>();
        firePS = GetComponentInChildren<ParticleSystem>();

    }

    // Update is called once per frame
    void Update()
    {
        if (!sizeChanging) // se non sto già cambiando size allora entro
        {
            sizeChanging = true; // evito di chiamare la coroutine di crescita più volte insieme
            StartCoroutine(GrowCoroutine());
        }

    }

    public void Extinguish()
    {
        if (!isExtinguishing)
        {
            isExtinguishing = true;
            StartCoroutine(ExtinguishingCoroutine());
        }
        //print("Fire " + gameObject.GetInstanceID() + " healt: " + fireHealt);
        if (fireHealt <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    private IEnumerator GrowCoroutine()
    {
        if (!isHit)
        {
            if (fireHealt < maxHealt)
            {
                fireHealt += 0.1f;
            }
        }

        float scaleFactor = fireHealt / maxHealt;
        Vector3 startScale = fireTran.localScale;
        Vector3 targetScale = initialScale * scaleFactor;
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // da 0 a 1
            fireTran.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null; // aspetta il prossimo frame
        }

        fireTran.localScale = targetScale;
        sizeChanging = false;
    }

    private IEnumerator ExtinguishingCoroutine()
    {
        fireHealt -= 0.3f;
        yield return new WaitForSeconds(3f);
        isExtinguishing = false;
    }

}
