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

    public ScrollRect scrollable;

    public Scrollbar verticalScrollbar;
    public RectTransform content;

    public SlidesDataSender sender;

    [Header("Canvas Transition")]
    public GameObject canvasToDisable;
    public GameObject canvasToEnable;

    public float fadeDuration = 1f;

    public AudioSource audioSource;

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

    public void UpdateScrollState()
    {
        if (scrollable == null || content == null)
        {
            Debug.LogWarning("[FeedbackScrollableController] ScrollRect o Content non assegnati.");
            return;
        }

        float contentHeight = content.rect.height;
        float viewportHeight = scrollable.viewport != null
            ? scrollable.viewport.rect.height
            : scrollable.GetComponent<RectTransform>().rect.height;

        bool needsScroll = contentHeight > viewportHeight + 10f;

        scrollable.enabled = needsScroll;
        if (verticalScrollbar != null)
            verticalScrollbar.gameObject.SetActive(needsScroll);

    }

    public void ResetScrollPosition()
    {
        if (scrollable == null)
            return;

        scrollable.verticalNormalizedPosition = 1f;   // in alto
        scrollable.horizontalNormalizedPosition = 0f; // a sinistra
        Canvas.ForceUpdateCanvases();

        //Debug.Log($"[FeedbackScrollableController] Scroll riallineato in alto per '{name}'.");
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
            ResetScrollPosition();
            UpdateScrollState();
            yield return null;

        }

        // Assicurati che arrivi esattamente al target
        transform.localScale = targetScale;
        scaleRoutine = null;


        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            // Force rebuild layout **dopo che la scala è stata applicata**
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);


            Canvas.ForceUpdateCanvases();
        }



        // Debug.Log($"[ResetScrollOnEnable] Reset scroll eseguito per '{gameObject.name}'");
    }


    public void CloseFeedback()
    {
        StartCoroutine(CloseFeedbackRoutine());

    }

    public IEnumerator CloseFeedbackRoutine()
    {
        if (audioSource != null)
            audioSource.Play();

        yield return StartCoroutine(FadeSwitch(canvasToDisable, canvasToEnable, fadeDuration));
        yield return new WaitForSeconds(1f);

        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        if (waypointInstance != null)
        {
            foreach (Transform child in waypointInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Question") { child.gameObject.SetActive(false); Debug.Log("[CloseFeedbackRoutine] 'Question' disattivato."); }
                if (child.name == "Check") { child.gameObject.SetActive(true); Debug.Log("[CloseFeedbackRoutine] 'Check' attivato."); }
            }

            yield return new WaitForSeconds(1f); // <-- lascia vedere il cambiamento

            Destroy(waypointInstance);
        }

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


    private void OnDestroy()
    {
        foreach (RectTransform child in content)
        {
            SlideData sd = child.GetComponent<SlideData>();
            if (sd != null)
            {
                sd.SendData();
            }
        }

        if (sender != null)
        {
            sender.SendData();
        }
        else
        {
            Debug.LogWarning("[OnDestroy] sender è NULL.");
        }
    }


        private IEnumerator FadeSwitch(GameObject toDisable, GameObject toEnable, float duration)
    {
        if (toDisable == null || toEnable == null)
            yield break;

        CanvasGroup cgDisable = toDisable.GetComponent<CanvasGroup>();
        CanvasGroup cgEnable = toEnable.GetComponent<CanvasGroup>();

        if (cgDisable == null)
            cgDisable = toDisable.AddComponent<CanvasGroup>();

        if (cgEnable == null)
            cgEnable = toEnable.AddComponent<CanvasGroup>();

        toEnable.SetActive(true);

        float elapsed = 0f;

        float startAlphaDisable = cgDisable.alpha;
        float startAlphaEnable = cgEnable.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            cgDisable.alpha = Mathf.Lerp(startAlphaDisable, 0f, t);
            cgEnable.alpha = Mathf.Lerp(startAlphaEnable, 1f, t);

            yield return null;
        }

        cgDisable.alpha = 0f;
        cgEnable.alpha = 1f;

        toDisable.SetActive(false);
    }

    public void SwitchCanvasWithFade()
    {
        StartCoroutine(FadeSwitch(canvasToDisable, canvasToEnable, fadeDuration));
    }




}
