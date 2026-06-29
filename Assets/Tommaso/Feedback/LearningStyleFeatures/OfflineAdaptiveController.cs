using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class OfflineAdaptiveController : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static OfflineAdaptiveController Instance { get; private set; }

    // ── Controllo globale ──────────────────────────────────────────────────
    [Header("Controllo globale")]
    [Tooltip("Se disattivato, il sistema adattivo non modifica mai il profilo.")]
    [SerializeField] private bool enableAdaptiveSystem = true;

    [Tooltip("Numero massimo di switch di stile (Attivo↔Riflessivo) permessi per sessione. 0 = illimitati.")]
    [SerializeField] private int maxStyleSwitches = 0;

    // ── Score adattivo ─────────────────────────────────────────────────────
    [Header("Score adattivo")]
    [SerializeField] private AdaptiveScore adaptiveScore = new AdaptiveScore();

    // ── Soglie feedback engagement ─────────────────────────────────────────
    [Header("Soglie osservazione pannelli")]
    [Tooltip("FocusTime minimo per il profilo Riflessivo: se l'utente guarda il feedback meno di questo tempo lo score riflessivo scende.")]
    [SerializeField] private float minFocusTimeRiflessivo = 8f;

    [Tooltip("FocusTime massimo per il profilo Attivo: se l'utente guarda il feedback più di questo tempo lo score attivo scende (score riflessivo sale).")]
    [SerializeField] private float maxFocusTimeAttivo = 5f;

    [System.Serializable]
    public class ExcludedChapterEntry
    {
        [ChapterDropdown]
        public string chapterName;
    }

    [Tooltip("Capitoli i cui feedback non vengono conteggiati per il calcolo dell'engagement.")]
    [SerializeField] private List<ExcludedChapterEntry> excludedFeedbackChapters = new List<ExcludedChapterEntry>();

    // ── Soglie bottone riflessivo ──────────────────────────────────────────
    [Header("Soglie bottone riflessivo (disabilita Learning Features)")]
    [SerializeField] private float minTimeBeforeReflectiveButton = 8f;
    [SerializeField] private int fastButtonsToDisable = 2;

    // ── Stato interno ──────────────────────────────────────────────────────
    private LearningProfile _learningProfile;
    private ChapterTracker  _chapterTracker;
    private ChapterTimer    _chapterTimer;
    private int  _fastButtonCount = 0;
    private int  _switchCount = 0;
    private bool _isOfflineSession = false;
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

    /// <summary>
    /// Chiamato da SessionManager.StartOfflineSession().
    /// Ripristina sempre lo score da SelectedLearningProfile.riflessivoScore,
    /// che viene inizializzato dalla scelta del menu in SessionManager.SetLearningProfile()
    /// se ancora al valore di default (0.5).
    /// </summary>
    public void InitForOfflineSession()
    {
        _isOfflineSession = true;
        _fastButtonCount  = 0;
        _switchCount      = 0;
        _pendingDisableLearningFeatures = false;

        var sel = SessionManager.Instance?.SelectedLearningProfile;
        if (sel != null)
        {
            adaptiveScore.score = sel.riflessivoScore;
            Debug.Log($"[OfflineAdaptiveController] Sessione offline — score: {adaptiveScore.score:F2} {adaptiveScore.Interpret()}");
        }
    }

    public void ResetForOnlineSession()
    {
        _isOfflineSession = false;
        _fastButtonCount  = 0;
        _switchCount      = 0;
        _pendingDisableLearningFeatures = false;
        UnsubscribeChapterTracker();
        Debug.Log("[OfflineAdaptiveController] Sessione online — sistema disattivato.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isOfflineSession) return;

        _learningProfile = FindFirstObjectByType<LearningProfile>();

        UnsubscribeChapterTracker();
        _chapterTracker = FindFirstObjectByType<ChapterTracker>();
        if (_chapterTracker != null)
            _chapterTracker.ObservationDataReady += OnChapterCompleted;
        _chapterTimer = FindFirstObjectByType<ChapterTimer>();

        Debug.Log($"[OfflineAdaptiveController] Scena '{scene.name}' — " +
                  $"score={adaptiveScore.score:F2} {adaptiveScore.Interpret()}");
    }

    private void UnsubscribeChapterTracker()
    {
        if (_chapterTracker != null)
            _chapterTracker.ObservationDataReady -= OnChapterCompleted;
        _chapterTracker = null;
    }

    private void OnDestroy() => UnsubscribeChapterTracker();

    // ── Entry point 1: chiusura feedback (da FeedbackAutoManager) ─────────

    public void OnFeedbackCompleted(SlidesDataSender sender, FeedbackPrefabController fpc, string chapterName)
    {
        if (!enableAdaptiveSystem || !_isOfflineSession) return;
        if (sender == null || fpc == null) return;

        if (_pendingDisableLearningFeatures)
        {
            _pendingDisableLearningFeatures = false;
            DisableLearningFeatures();
            return;
        }

        // Ignora i feedback dei capitoli esclusi
        if (excludedFeedbackChapters.Any(e => e.chapterName == chapterName))
        {
            Debug.Log($"[OfflineAdaptiveController] Feedback '{sender.FeedbackName}' ignorato (capitolo '{chapterName}' escluso).");
            return;
        }

        EvaluateFeedbackEngagement(sender, fpc);
    }

    // ── Entry point 2: completamento capitolo (da ChapterTracker) ─────────

    private void OnChapterCompleted(string guid, string chapterName, int errors, float time)
    {
        if (!enableAdaptiveSystem || !_isOfflineSession) return;

        // Leggi midEventTime dal ChapterTimer per questo capitolo.
        // Se non configurato o midEventTime è 0, usa defaultSlowTimeThreshold come fallback.
        float midEventTime = GetMidEventTime(chapterName);
        if (midEventTime <= 0f)
        {
            Debug.LogWarning($"[OfflineAdaptiveController] midEventTime non configurato per '{chapterName}', capitolo ignorato.");
            return;
        }

        // Calcola il moltiplicatore continuo: 0 = completato subito, 1 = completato esattamente al midEvent, >1 = oltre
        // Clampato a [0, 2] per evitare delta eccessivi.
        float slowRatio = Mathf.Clamp(time / midEventTime, 0f, 2f);

        Debug.Log($"[OfflineAdaptiveController] Capitolo '{chapterName}': " +
                  $"time={time:F1}s, midEventTime={midEventTime:F1}s, slowRatio={slowRatio:F2}");

        // Applica delta scalato: più ci si avvicina (o supera) il midEventTime, più lo score aumenta.
        // Se il ratio è basso (capitolo veloce), lo score diminuisce proporzionalmente.
        adaptiveScore.ApplySlowChapterContinuous(slowRatio);

        PersistScore();
        EvaluateSwitch();
    }

    private float GetMidEventTime(string chapterName)
    {
        if (_chapterTimer == null) return 0f;
        var setting = _chapterTimer.timerSettings
            .FirstOrDefault(s => s.chapterWithTimer == chapterName);
        return setting?.midEventTime ?? 0f;
    }

    // ── Entry point 3: bottone riflessivo (da FeedbackPrefabController) ───

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
            Debug.Log($"[OfflineAdaptiveController] Bottone rapido. Count: {_fastButtonCount}/{fastButtonsToDisable}");
            if (_fastButtonCount >= fastButtonsToDisable)
            {
                _pendingDisableLearningFeatures = true;
                _fastButtonCount = 0;
                Debug.Log("[OfflineAdaptiveController] Disabilitazione in attesa chiusura feedback.");
            }
        }
        // Bottone lento: il contatore non si azzera, si accumula nel tempo
    }

    // ── Valutazione engagement feedback ───────────────────────────────────

    private void EvaluateFeedbackEngagement(SlidesDataSender sender, FeedbackPrefabController fpc)
    {
        if (_learningProfile == null) return;

        float totalFocusTime = sender.GetCurrentTotalFocusTime();

        int totalPages  = 0;
        int pagesOpened = 0;

        if (fpc.content != null)
        {
            foreach (RectTransform child in fpc.content)
            {
                SlideData sd = child.GetComponent<SlideData>();
                if (sd == null || sd.isIntroductory) continue;
                totalPages++;
                if (sd.getOpening() > 0) pagesOpened++;
            }
        }

        bool pagesNotExplored = totalPages > 0 && pagesOpened < totalPages;
        var currentAxis = _learningProfile.attivoRiflessivo;

        Debug.Log($"[OfflineAdaptiveController] '{sender.FeedbackName}': " +
                  $"focusTime={totalFocusTime:F2}s, pagine={pagesOpened}/{totalPages}, " +
                  $"profilo={currentAxis}");

        if (currentAxis == LearningEnums.AttivoRiflessivo.Riflessivo)
        {
            // Profilo Riflessivo: se guarda poco o non esplora tutte le pagine → score scende
            bool lowEngagement = totalFocusTime < minFocusTimeRiflessivo || pagesNotExplored;
            if (lowEngagement)
            {
                Debug.Log($"[OfflineAdaptiveController] Riflessivo: engagement basso → score scende.");
                adaptiveScore.ApplyLowFeedback();
            }
            else
            {
                Debug.Log($"[OfflineAdaptiveController] Riflessivo: engagement alto → score sale.");
                adaptiveScore.ApplyHighFeedback();
            }
        }
        else
        {
            // Profilo Attivo: se guarda troppo a lungo → score sale (verso Riflessivo)
            bool highEngagement = totalFocusTime > maxFocusTimeAttivo && !pagesNotExplored;
            if (highEngagement)
            {
                Debug.Log($"[OfflineAdaptiveController] Attivo: engagement alto → score sale.");
                adaptiveScore.ApplyHighFeedback();
            }
            else
            {
                Debug.Log($"[OfflineAdaptiveController] Attivo: engagement basso → score scende.");
                adaptiveScore.ApplyLowFeedback();
            }
        }

        PersistScore();
        EvaluateSwitch();
    }

    // ── Decisione di switch ────────────────────────────────────────────────

    private void EvaluateSwitch()
    {
        if (_learningProfile == null) return;

        // Se il limite di switch è stato raggiunto, non cambiare più
        if (maxStyleSwitches > 0 && _switchCount >= maxStyleSwitches)
        {
            Debug.Log($"[OfflineAdaptiveController] Limite switch raggiunto ({_switchCount}/{maxStyleSwitches}), nessun cambio.");
            return;
        }

        var current = _learningProfile.attivoRiflessivo;

        if (current == LearningEnums.AttivoRiflessivo.Riflessivo && adaptiveScore.ShouldSwitchToAttivo())
            SwitchToAttivo();
        else if (current == LearningEnums.AttivoRiflessivo.Attivo && adaptiveScore.ShouldSwitchToRiflessivo())
            SwitchToRiflessivo();
    }

    // ── Switch Riflessivo → Attivo ─────────────────────────────────────────

    private void SwitchToAttivo()
    {
        Debug.Log("[OfflineAdaptiveController] *** Switch: Riflessivo → Attivo ***");
        _switchCount++;

        if (_learningProfile.riflessivoFeatures is RiflessivoFeatures rf && RiflessivoFeatures.IsPaused)
            rf.EmergencyReset();

        _learningProfile.attivoRiflessivo = LearningEnums.AttivoRiflessivo.Attivo;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.attivoRiflessivo =
                LearningEnums.AttivoRiflessivo.Attivo;
    }

    // ── Switch Attivo → Riflessivo ─────────────────────────────────────────

    private void SwitchToRiflessivo()
    {
        Debug.Log("[OfflineAdaptiveController] *** Switch: Attivo → Riflessivo ***");
        _switchCount++;

        // enableLearningFeatures sempre disabilitato per chi arriva da Attivo
        _learningProfile.enableLearningFeatures = false;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.enableLearningFeatures = false;

        _learningProfile.attivoRiflessivo = LearningEnums.AttivoRiflessivo.Riflessivo;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.attivoRiflessivo =
                LearningEnums.AttivoRiflessivo.Riflessivo;
    }

    // ── Disabilitazione Learning Features ─────────────────────────────────

    private void DisableLearningFeatures()
    {
        if (_learningProfile == null) return;
        if (!_learningProfile.enableLearningFeatures) return;

        Debug.Log("[OfflineAdaptiveController] *** Disabilitazione Learning Features ***");

        if (_learningProfile.riflessivoFeatures is RiflessivoFeatures rf && RiflessivoFeatures.IsPaused)
            rf.EmergencyReset();

        _learningProfile.enableLearningFeatures = false;

        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.enableLearningFeatures = false;
    }

    // ── Persistenza score ──────────────────────────────────────────────────

    private void PersistScore()
    {
        if (SessionManager.Instance?.SelectedLearningProfile != null)
            SessionManager.Instance.SelectedLearningProfile.riflessivoScore = adaptiveScore.score;
    }


}