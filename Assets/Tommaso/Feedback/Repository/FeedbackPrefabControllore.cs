using System.Collections;
using UnityEngine;
using UnityEngine.UI; // per il componente Button


public class FeedbackPrefabController : MonoBehaviour
{
    [Header("Parametri generali (configurabili da Inspector)")]
    public GameObject waypointPrefab;
    public GameObject OptionalWayPoint;
    public float activationDistance = 1f;
    public float scaleSpeed = 3f;
    public float maxScale = 0.004f;
    public bool stayOpen = false;

    public bool isOptionalFeedback = false;

    private LearningStyleFeatures styleBehaviour;
    private LearningProfile profile;

    private GameObject waypointInstance;
    private Camera playerCamera;
    private Coroutine scaleRoutine;
    private bool isVisible = false;

 //   public Button reflectiveButtonActivate;

 //   public Button reflectiveButtonDeactivate;

    public ScrollRect scrollable;

    public Scrollbar verticalScrollbar;
    public RectTransform content;

    private bool _reflectiveEffectsConsumed = false;

    private bool isClosing = false;

    public SlidesDataSender sender;

    [Header("Canvas Transition")]
    public GameObject canvasToDisable;
    public GameObject canvasToEnable;

    public float fadeDuration = 1f;

    public AudioSource audioSource;

    public bool needsButtonToBeCompleted = false;

    public GameObject buttonsToClickCanvas;

    public bool applyReflectiveEffects = true;



    private void Start()
    {
        if (buttonsToClickCanvas != null)
        {
            if (needsButtonToBeCompleted)
                buttonsToClickCanvas.SetActive(true);
            else buttonsToClickCanvas.SetActive(false);
        }

        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        profile = FindFirstObjectByType<LearningProfile>();
        ////Debug.Log($"[FeedbackPrefabController] profile trovato: {profile != null}, applyReflectiveEffects: {applyReflectiveEffects}");

        if (profile != null && applyReflectiveEffects)
        {
            styleBehaviour = profile.GetCurrentBehaviour();
            ////Debug.Log($"[FeedbackPrefabController] styleBehaviour: {styleBehaviour?.GetType().Name ?? "NULL"}");

            if (styleBehaviour is RiflessivoFeatures)
            {
                ////Debug.Log("[FeedbackPrefabController] Profilo RIFLESSIVO rilevato.");

                if (buttonsToClickCanvas != null)
                {
                    buttonsToClickCanvas.SetActive(true);
                    ////Debug.Log("[FeedbackPrefabController] buttonsToClickCanvas attivato.");

                    Button[] buttons = buttonsToClickCanvas.GetComponentsInChildren<Button>(true);
                    ////Debug.Log($"[FeedbackPrefabController] Bottoni trovati: {buttons.Length}");

                    foreach (Button btn in buttons)
                    {
                        btn.onClick.AddListener(OnReflectiveButtonClicked);
                        ////Debug.Log($"[FeedbackPrefabController] Listener aggiunto al bottone: {btn.name}");
                    }
                }
                else
                {
                  //  //////Debug.LogWarning("[FeedbackPrefabController] buttonsToClickCanvas è NULL, impossibile attivarlo.");
                }
            }
            else
            {
             //   //////Debug.Log("[FeedbackPrefabController] Profilo NON riflessivo, nessun bottone attivato.");
            }
        }
        else
        {
            //////Debug.LogWarning($"[FeedbackPrefabController] Blocco riflessivo saltato — profile null: {profile == null}, applyReflectiveEffects: {applyReflectiveEffects}");
        }

        if (isOptionalFeedback)
        {
            if (OptionalWayPoint != null)
            {
                waypointInstance = Instantiate(OptionalWayPoint, transform.position, Quaternion.identity);
                waypointInstance.name = $"OptionalWaypoint_{name}";
                waypointInstance.SetActive(true);
            }
        }
        else if (waypointPrefab != null)
        {
            waypointInstance = Instantiate(waypointPrefab, transform.position, Quaternion.identity);
            waypointInstance.name = $"Waypoint_{name}";
            waypointInstance.SetActive(true);
        }

        transform.localScale = Vector3.zero;
        styleBehaviour?.resetVariables();
        StartCoroutine(CheckDistanceRoutine());
    }

    private IEnumerator CheckDistanceRoutine()
    {

        while (playerCamera != null)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, transform.position);

        if (distance <= activationDistance && !isVisible && !isClosing)
        {
            isVisible = true;
            if (waypointInstance != null)
                waypointInstance.SetActive(false);

            StartScaling(Vector3.one * maxScale);
            if (!_reflectiveEffectsConsumed)
                styleBehaviour?.OnFeedbackOpened(this);
        }
        else if (distance > activationDistance && isVisible && !isClosing)
        {
            isVisible = false;
            if (waypointInstance != null)
                waypointInstance.SetActive(true);

            StartScaling(Vector3.zero);
            if (!_reflectiveEffectsConsumed)
                styleBehaviour?.OnFeedbackClosed(this);
        }
            yield return null;
        }
    }

    private void OnReflectiveButtonClicked()
    {
        _reflectiveEffectsConsumed = true;
        styleBehaviour?.OnFeedbackClosed(this);
    }

    public void UpdateScrollState()
    {
        if (scrollable == null || content == null)
        {
            //////Debug.LogWarning("[FeedbackScrollableController] ScrollRect o Content non assegnati.");
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

        ////////Debug.Log($"[FeedbackScrollableController] Scroll riallineato in alto per '{name}'.");
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

        if (stayOpen)
        {
            DisableFeedbackCorutine();
        }

        // //////Debug.Log($"[ResetScrollOnEnable] Reset scroll eseguito per '{gameObject.name}'");
    }

    public void DisableFeedbackCorutine()
    {
        if(waypointInstance != null)
        {
            waypointInstance.gameObject.SetActive(false);
        }
        StopAllCoroutines();
    }

    public void CloseFeedback()
    {
        StartCoroutine(CloseFeedbackRoutine());

    }

    public IEnumerator CloseFeedbackRoutine()
    {
        isClosing = true;

        if (audioSource != null)
            audioSource.Play();

        yield return StartCoroutine(FadeSwitch(canvasToDisable, canvasToEnable, fadeDuration));
        yield return new WaitForSeconds(1f);

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        if (waypointInstance != null)
        {
            foreach (Transform child in waypointInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Question") child.gameObject.SetActive(false);
                if (child.name == "Check") child.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(1f);
            Destroy(waypointInstance);
        }

        styleBehaviour?.OnFeedbackClosed(this);

        // Scala a zero garantita
        scaleRoutine = StartCoroutine(SmoothScale(Vector3.zero));
        StartCoroutine(DestroyAfterClose()); // con timeout
    }

    public void CloseFeedbackWithoutCompletion()
    {
        StartCoroutine(CloseFeedbackWithoutCompletionRoutine());
    }

    private IEnumerator CloseFeedbackWithoutCompletionRoutine()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        if (waypointInstance != null)
            Destroy(waypointInstance);

        scaleRoutine = StartCoroutine(SmoothScale(Vector3.zero));

        while (transform.localScale.magnitude > 0.0001f)
            yield return null;

        Destroy(gameObject);
    }

    private IEnumerator DestroyAfterClose()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (transform.localScale.magnitude > 0.0001f && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

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
            //////Debug.LogWarning("[OnDestroy] sender è NULL.");
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
