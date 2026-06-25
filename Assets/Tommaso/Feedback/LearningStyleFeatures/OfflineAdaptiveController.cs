using UnityEngine;
using UnityEngine.SceneManagement;

public class OfflineAdaptiveController : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static OfflineAdaptiveController Instance { get; private set; }

    // ── Controllo globale ──────────────────────────────────────────────────
    [Header("Controllo globale")]
    [Tooltip("Se disattivato, il sistema adattivo non modifica mai il profilo.")]
    [SerializeField] private bool enableAdaptiveSystem = true;

    // ── Soglie asse Attivo/Riflessivo ──────────────────────────────────────
    [Header("Soglie osservazione pannelli (Riflessivo → Attivo)")]
    [SerializeField] private float minFocusTimeThreshold = 5f;
    [SerializeField] private int consecutiveFeedbacksToSwitch = 2;

    // ── Soglie disabilitazione Learning Features ───────────────────────────
    [Header("Soglie bottone riflessivo (disabilita Learning Features)")]
    [Tooltip("Secondi minimi tra primo gaze e pressione del bottone riflessivo.")]
    [SerializeField] private float minTimeBeforeReflectiveButton = 8f;
    [Tooltip("Quante volte consecutive troppo veloci prima di disabilitare le features.")]
    [SerializeField] private int consecutiveFastButtonsToDisable = 2;

    // ── Stato interno ──────────────────────────────────────────────────────
    private LearningProfile _learningProfile;
    private int _lowEngagementCount = 0;
    private int _fastButtonCount = 0;
    private bool _isOfflineSession = false;

    /// <summary>
    /// True quando il contatore fast-button ha raggiunto la soglia ma stiamo
    /// aspettando che il feedback corrente si chiuda prima di disabilitare.
    /// </summary>
    private bool _pendingDisableLearningFeatures = false;

    // ── Ciclo di vita ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void InitForOfflineSession()
    {
        _isOfflineSession = true;
        _lowEngagementCount = 0;
        _fastButtonCount = 0;
        _pendingDisableLearningFeatures = false;
        Debug.Log("[OfflineAdaptiveController] Sessione offline — sistema attivato.");
    }

    public void ResetForOnlineSession()
    {
        _isOfflineSession = false;
        _lowEngagementCount = 0;
        _fastButtonCount = 0;
        _pendingDisableLearningFeatures = false;
        Debug.Log("[OfflineAdaptiveController] Sessione online — sistema disattivato.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isOfflineSession) return;

        _learningProfile = FindFirstObjectByType<LearningProfile>();
        _lowEngagementCount = 0;

        Debug.Log($"[OfflineAdaptiveController] Scena '{scene.name}' — " +
                  $"LearningProfile {(_learningProfile != null ? "trovato" : "NON trovato")}.");
    }

    // ── Entry point 1: chiusura feedback (da FeedbackAutoManager) ─────────

    public void OnFeedbackCompleted(SlidesDataSender sender, FeedbackPrefabController fpc)
    {
        if (!enableAdaptiveSystem || !_isOfflineSession) return;
        if (sender == null || fpc == null) return;

        // Il feedback si sta chiudendo: se era pendente la disabilitazione, ora è il momento
        if (_pendingDisableLearningFeatures)
        {
            _pendingDisableLearningFeatures = false;
            DisableLearningFeatures();
            return; // non valutare anche l'engagement su questo feedback
        }

        EvaluateFeedback(sender, fpc);
    }

    // ── Entry point 2: bottone riflessivo premuto (da FeedbackPrefabController) ──

    public void OnReflectiveButtonPressed(SlidesDataSender sender)
    {
        if (!enableAdaptiveSystem || !_isOfflineSession) return;
        if (sender == null) return;

        float firstGaze = sender.firstGazeTimestamp;
        float timeBeforeButton = firstGaze >= 0f ? Time.time - firstGaze : 0f;

        Debug.Log($"[OfflineAdaptiveController] Bottone riflessivo '{sender.FeedbackName}': " +
                  $"timeBeforeButton={timeBeforeButton:F2}s (soglia={minTimeBeforeReflectiveButton}s)");

        if (timeBeforeButton < minTimeBeforeReflectiveButton)
        {
            _fastButtonCount++;
            Debug.Log($"[OfflineAdaptiveController] Bottone rapido. Count: {_fastButtonCount}/{consecutiveFastButtonsToDisable}");

            if (_fastButtonCount >= consecutiveFastButtonsToDisable)
            {
                // Non disabilito subito: aspetto che il feedback si chiuda (OnFeedbackCompleted)
                _pendingDisableLearningFeatures = true;
                _fastButtonCount = 0;
                Debug.Log("[OfflineAdaptiveController] Disabilitazione in attesa chiusura feedback.");
            }
        }
        else
        {
            _fastButtonCount = 0;
        }
    }

    // ── Logica di valutazione pannelli ─────────────────────────────────────

    private void EvaluateFeedback(SlidesDataSender sender, FeedbackPrefabController fpc)
    {
        float totalFocusTime = sender.GetCurrentTotalFocusTime();

        int totalPages = 0;
        int pagesOpened = 0;

        if (fpc.content != null)
        {
            foreach (RectTransform child in fpc.content)
            {
                SlideData sd = child.GetComponent<SlideData>();
                if (sd == null || sd.isIntroductory) continue;

                totalPages++;
                if (sd.getOpening() > 0)
                    pagesOpened++;
            }
        }

        bool lowFocus         = totalFocusTime < minFocusTimeThreshold;
        bool pagesNotExplored = totalPages > 0 && pagesOpened < totalPages;
        bool isLowEngagement  = lowFocus || pagesNotExplored;

        Debug.Log($"[OfflineAdaptiveController] '{sender.FeedbackName}': " +
                  $"focusTime={totalFocusTime:F2}s, pagine={pagesOpened}/{totalPages}, " +
                  $"lowEngagement={isLowEngagement}");

        if (isLowEngagement)
        {
            _lowEngagementCount++;
            Debug.Log($"[OfflineAdaptiveController] Low-engagement: {_lowEngagementCount}/{consecutiveFeedbacksToSwitch}");
            if (_lowEngagementCount >= consecutiveFeedbacksToSwitch)
                SwitchToAttivo();
        }
        else
        {
            _lowEngagementCount = 0;
        }
    }

    // ── Switch asse Attivo/Riflessivo ──────────────────────────────────────

    private void SwitchToAttivo()
    {
        if (_learningProfile == null) return;

        if (_learningProfile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo)
        {
            Debug.Log("[OfflineAdaptiveController] Già Attivo.");
            _lowEngagementCount = 0;
            return;
        }

        Debug.Log("[OfflineAdaptiveController] *** Switch: Riflessivo → Attivo ***");

        if (_learningProfile.riflessivoFeatures is RiflessivoFeatures rf && RiflessivoFeatures.IsPaused)
            rf.EmergencyReset();

        _learningProfile.attivoRiflessivo = LearningEnums.AttivoRiflessivo.Attivo;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.attivoRiflessivo =
                LearningEnums.AttivoRiflessivo.Attivo;

        _lowEngagementCount = 0;
    }

    // ── Disabilitazione Learning Features ─────────────────────────────────

    private void DisableLearningFeatures()
    {
        if (_learningProfile == null) return;

        if (!_learningProfile.enableLearningFeatures)
        {
            Debug.Log("[OfflineAdaptiveController] Learning Features già disabilitate.");
            return;
        }

        Debug.Log("[OfflineAdaptiveController] *** Disabilitazione Learning Features ***");

        if (_learningProfile.riflessivoFeatures is RiflessivoFeatures rf && RiflessivoFeatures.IsPaused)
            rf.EmergencyReset();

        _learningProfile.enableLearningFeatures = false;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.enableLearningFeatures = false;
    }
}