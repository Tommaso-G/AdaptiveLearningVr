using System.Collections;
using UnityEngine;
using UnityEngine.UI; // per il componente Button
using Unity.VisualScripting;

public class FeedbackPrefabController : MonoBehaviour
{
    [Header("Parametri generali (configurabili da Inspector)")]
    public GameObject waypointPrefab;
    public float activationDistance = 2f;
    public float scaleSpeed = 3f;
    public float maxScale = 0.004f;

    [HideInInspector]
    public bool WasClicked = false;

    private LearningStyleFeatures styleBehaviour;
    private LearningProfile profile;

    private GameObject waypointInstance;
    private Camera playerCamera;
    private Coroutine scaleRoutine;
    private bool isVisible = false;

    public Button reflectiveButton; // bottone per profilo riflessivo

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
                ActivateReflectiveButton();
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

        // Avvia controllo distanza
        StartCoroutine(CheckDistanceRoutine());
    }

    private void ActivateReflectiveButton()
    {

        if (reflectiveButton != null)
        {
            reflectiveButton.gameObject.SetActive(true);
            reflectiveButton.onClick.RemoveAllListeners();
            reflectiveButton.onClick.AddListener(OnClicked);

            Debug.Log($"[FeedbackPrefabController] Bottone riflessivo attivato per '{name}'.");
        }
        else
        {
            Debug.LogWarning($"[FeedbackPrefabController] Nessun bottone trovato in '{name}' per il profilo riflessivo.");
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

    public void OnClicked()
    {
        if (WasClicked) return;

        WasClicked = true;
        Debug.Log($"[FeedbackPrefabController] Feedback '{name}' cliccato.");

        styleBehaviour?.GetType().GetMethod("OnFeedbackClicked")?
            .Invoke(styleBehaviour, new object[] { this });
    }
}
