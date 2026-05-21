    using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

public enum FireType
{
    Classe_Solido,   // solidi
    Classe_Liquido,   // liquidi
    Classe_Elettrico,   // elettrico
}
public class FireObject : MonoBehaviour, IDestructible
{
    [SerializeField] private FireType fireType;
    [SerializeField] private Transform fireTran;
    [SerializeField] private BoxCollider fireBox;
    [SerializeField] private SphereCollider damageArea;
    [SerializeField] private ParticleSystem firePS;
    [SerializeField] private float fireHealt = 1f;
    [SerializeField] private float maxHealt = 1f;
    [SerializeField] private float fireDamage;
    [SerializeField] private float extinguishSpeed = 0.3f;
    [SerializeField] private float growSpeed = 0.1f;
    private Vector3 initialScale;
    private bool sizeChanging = false;
    private bool isExtinguishing = false;
    public bool isHit = false;
    public FireType FireType => fireType;


    public event System.Action OnDestroyed;


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
    void Update()
    {
        if (!sizeChanging)
        {
            sizeChanging = true;
            StartCoroutine(UpdateFireCoroutine());
        }
    }

    private IEnumerator UpdateFireCoroutine()
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // aggiorna healt ogni frame in base a isHit
            if (isHit)
            {
                fireHealt -= extinguishSpeed * Time.deltaTime;
                fireHealt = Mathf.Max(fireHealt, 0f);
            }
            else
            {
                fireHealt += growSpeed * Time.deltaTime;
                fireHealt = Mathf.Min(fireHealt, maxHealt);
            }

            // scala segue fireHealt in tempo reale
            float scaleFactor = fireHealt / maxHealt;
            fireTran.localScale = initialScale * scaleFactor;

            if (fireHealt <= 0)
            {
                OnDestroyed?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }

        sizeChanging = false;
    }

}
