using System.Collections;
using UnityEngine;
using UnityEngine.UI; // per il componente Button


public class FeedbackPrefabController : MonoBehaviour
{
    [Header("Parametri generali (configurabili da Inspector)")]
    public GameObject waypointPrefab;
    public float activationDistance = 1f;
    public float scaleSpeed = 3f;
    public float maxScale = 0.004f;

    private LearningStyleFeatures styleBehaviour;
    private LearningProfile profile;

    private GameObject waypointInstance;
    private Camera playerCamera;
    private Coroutine scaleRoutine;
    private bool isVisible = false;

    public Button reflectiveButtonActivate;

    public Button reflectiveButtonDeactivate;

    private void Start()
    {
        // Trova la camera del giocatore
        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        // Recupera il profilo di apprendimento
        profile = FindFirstObjectByType<LearningProfile>();
        if (profile != null)
        {
            styleBehaviour = profile.GetCurrentBehaviour(); // Ottiene lo stile attuale

            // Se il profilo è Riflessivo → cerca e attiva il bottone
            if (styleBehaviour is RiflessivoFeatures)
            {
                HandleReflectiveButtons();
            }
        }

        // Istanzia il waypoint
        if (waypointPrefab != null)
        {
            waypointInstance = Instantiate(waypointPrefab, transform.position, Quaternion.identity);
            waypointInstance.name = $"Waypoint_{name}";
            waypointInstance.SetActive(true);
        }

        // Imposta stato iniziale
        transform.localScale = Vector3.zero;
        styleBehaviour?.resetVariables();
        // Avvia controllo distanza
        StartCoroutine(CheckDistanceRoutine());

        
    }

    private void HandleReflectiveButtons()
    {
        if (reflectiveButtonActivate != null && reflectiveButtonDeactivate != null)
        {
            // Stato iniziale: mostra solo A
            reflectiveButtonActivate.gameObject.SetActive(false);
            reflectiveButtonDeactivate.gameObject.SetActive(true);

            // Rimuove listener vecchi e aggiunge i nuovi
            reflectiveButtonActivate.onClick.RemoveAllListeners();
            reflectiveButtonDeactivate.onClick.RemoveAllListeners();

            // Quando clicchi A → mostra B e nasconde A, 
            reflectiveButtonActivate.onClick.AddListener(() =>
            {
                reflectiveButtonActivate.gameObject.SetActive(false);
                reflectiveButtonDeactivate.gameObject.SetActive(true);
                Debug.Log("[FeedbackPrefabController] Bottone A cliccato, mostra B.");
                styleBehaviour?.EnableFeature();
                
            });

            // Quando clicchi B → mostra A e nasconde B
            reflectiveButtonDeactivate.onClick.AddListener(() =>
            {
                reflectiveButtonDeactivate.gameObject.SetActive(false);
                reflectiveButtonActivate.gameObject.SetActive(true);
                styleBehaviour?.DisableFeature();
                Debug.Log("[FeedbackPrefabController] Bottone B cliccato, mostra A.");
            });

            Debug.Log($"[FeedbackPrefabController] Bottoni riflessivi attivati per '{name}'.");
        }
        else
        {
            Debug.LogWarning($"[FeedbackPrefabController] Mancano uno o entrambi i bottoni riflessivi in '{name}'.");
        }
    }

    private IEnumerator CheckDistanceRoutine()
    {
        while (playerCamera != null)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, transform.position);

            if (distance <= activationDistance && !isVisible)
            {
                isVisible = true;
                if (waypointInstance != null)
                    waypointInstance.SetActive(false);

                StartScaling(Vector3.one * maxScale);
                styleBehaviour?.OnFeedbackOpened(this);
            }
            else if (distance > activationDistance && isVisible)
            {
                isVisible = false;
                if (waypointInstance != null)
                    waypointInstance.SetActive(true);

                StartScaling(Vector3.zero);
                styleBehaviour?.OnFeedbackClosed(this);
            }

            yield return null;
        }
    }

    private void StartScaling(Vector3 targetScale)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(SmoothScale(targetScale));
    }

    private IEnumerator SmoothScale(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.0001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleRoutine = null;
    }

    public void CloseFeedback()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        if (waypointInstance != null)
            Destroy(waypointInstance);

        scaleRoutine = StartCoroutine(SmoothScale(Vector3.zero));
        StartCoroutine(DestroyAfterClose());

        styleBehaviour?.OnFeedbackClosed(this);

    }

    private IEnumerator DestroyAfterClose()
    {
        while (transform.localScale.magnitude > 0.0001f)
            yield return null;

        Destroy(gameObject);
    
    }



}
