// MenuUI.cs (nella scena MenuScene)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuUI : MonoBehaviour
{
    // ── Pannello 1: principale ────────────────────────────────────────────
    [Header("Pannello principale")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button newSessionButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button levelsButton;           // apre il pannello livelli
    [SerializeField] private TextMeshProUGUI sessionInfoText;
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private Button learningStyleButton;

    // ── Pannello 2: selezione livello ─────────────────────────────────────
    [Header("Pannello livelli")]
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private Button levelsBackButton;
    [SerializeField] private OfflineLevelRegistry offlineLevelRegistry;

    // ── Pannello 3: stile di apprendimento ────────────────────────────────
    [Header("Pannello stile di apprendimento")]
    [SerializeField] private GameObject learningStylePanel;

    // Attivo / Riflessivo
    [SerializeField] private Button attivoButton;
    [SerializeField] private Button riflessivoButton;

    // Sensitivo / Intuitivo
    [SerializeField] private Button sensitivoButton;
    [SerializeField] private Button intuitivoButton;

    // Visivo / Verbale
    [SerializeField] private Button visivoButton;
    [SerializeField] private Button verbaleButton;

    // Sequenziale / Globale
    [SerializeField] private Button sequenzialeButton;
    [SerializeField] private Button globaleButton;

    [SerializeField] private Button learningBackButton;         // "Inizia"

    // ── Colori feedback selezione ─────────────────────────────────────────
    [Header("Colori bottoni stile")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color unselectedColor = Color.white;

    // ── Stato interno ─────────────────────────────────────────────────────
    private readonly List<Button> _spawnedLevelButtons = new List<Button>();
    private OfflineLevelConfig _pendingLevel;
    private LearningProfileSelection _profile = new LearningProfileSelection();

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Disabilita tutti gli outline nella scena (comportamento preesistente)
        foreach (var outline in FindObjectsByType<Outline>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            outline.enabled = false;
        }

        // Stato sessione online
        bool hasSavedSession = SessionPersistence.HasSavedSession();
        continueButton.interactable = hasSavedSession;
        sessionInfoText.text = hasSavedSession
            ? $"Sessione salvata: {SessionPersistence.Load()}"
            : "Nessuna sessione salvata";
        instructionsText.text = "Scegli un'opzione per iniziare:";

        // Listener pannello principale
        newSessionButton.onClick.AddListener(OnNewSessionClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        if (levelsButton != null)
            levelsButton.onClick.AddListener(() => ShowPanel(levelsPanel));

        // Listener pannello livelli
        if (levelsBackButton != null)
            levelsBackButton.onClick.AddListener(() => ShowPanel(mainPanel));
        BuildLevelButtons();

        // Listener pannello stile apprendimento
        attivoButton.onClick.AddListener(() => SetAttivoRiflessivo(LearningEnums.AttivoRiflessivo.Attivo));
        riflessivoButton.onClick.AddListener(() => SetAttivoRiflessivo(LearningEnums.AttivoRiflessivo.Riflessivo));

        sensitivoButton.onClick.AddListener(() => SetSensitivoIntuitivo(LearningEnums.SensitivoIntuitivo.Sensitivo));
        intuitivoButton.onClick.AddListener(() => SetSensitivoIntuitivo(LearningEnums.SensitivoIntuitivo.Intuitivo));

        visivoButton.onClick.AddListener(() => SetVisivoVerbale(LearningEnums.VisivoVerbale.Visivo));
        verbaleButton.onClick.AddListener(() => SetVisivoVerbale(LearningEnums.VisivoVerbale.Verbale));

        sequenzialeButton.onClick.AddListener(() => SetSequenzialeGlobale(LearningEnums.SequenzialeGlobale.Sequenziale));
        globaleButton.onClick.AddListener(() => SetSequenzialeGlobale(LearningEnums.SequenzialeGlobale.Globale));

        if (learningBackButton != null)
            learningBackButton.onClick.AddListener(() => ShowPanel(mainPanel));
        if (learningStyleButton != null)
            learningStyleButton.onClick.AddListener(OnLearningStyleSelected);

        if (SessionManager.Instance != null)
        {
            _profile = SessionManager.Instance.SelectedLearningProfile;
        }

        // Stato iniziale pannelli
        ShowPanel(mainPanel);

        // Aggiorna colori bottoni stile al valore default
        RefreshLearningStyleUI();
    }

    // ── Navigazione pannelli ──────────────────────────────────────────────

    private void ShowPanel(GameObject target)
    {
        if (mainPanel != null) mainPanel.SetActive(mainPanel == target);
        if (levelsPanel != null) levelsPanel.SetActive(levelsPanel == target);
        if (learningStylePanel != null) learningStylePanel.SetActive(learningStylePanel == target);

        if (target == mainPanel)
            instructionsText.text = "Scegli un'opzione per iniziare:";
        else if (target == levelsPanel)
            instructionsText.text = "Seleziona un livello:";
        else if (target == learningStylePanel)
            instructionsText.text = "Scegli il tuo stile di apprendimento:";
    }

    // ── Pannello principale ───────────────────────────────────────────────

    private void OnNewSessionClicked()
    {
        Debug.Log($"[MenuUI] Nuova sessione avviata " +
                  $"| {_profile.attivoRiflessivo} | {_profile.sensitivoIntuitivo} " +
                  $"| {_profile.visivoVerbale} | {_profile.sequenzialeGlobale}");
        SessionManager.Instance.SetLearningProfile(_profile);
        SessionManager.Instance.StartNewSession();
    }

    private void OnContinueClicked()
    {
        if (SessionPersistence.HasSavedSession())
        {
            Debug.Log($"[MenuUI] Continua sessione " +
                      $"| {_profile.attivoRiflessivo} | {_profile.sensitivoIntuitivo} " +
                      $"| {_profile.visivoVerbale} | {_profile.sequenzialeGlobale}");

            SessionManager.Instance.SetLearningProfile(_profile);
            SessionManager.Instance.ContinueSession();
        }
        else
        {
            Debug.LogWarning("[MenuUI] Nessuna sessione da continuare!");
        }
    }

    // ── Pannello livelli ──────────────────────────────────────────────────

    private void BuildLevelButtons()
    {
        if (offlineLevelRegistry == null || levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogWarning("[MenuUI] Configurazione livelli incompleta nell'Inspector.");
            return;
        }

        foreach (var b in _spawnedLevelButtons)
            if (b != null) Destroy(b.gameObject);
        _spawnedLevelButtons.Clear();

        foreach (OfflineLevelConfig level in offlineLevelRegistry.levels)
        {
            if (level == null) continue;

            Button btn = Instantiate(levelButtonPrefab, levelButtonContainer);

            var texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1) texts[0].text = level.levelName;
            if (texts.Length >= 2) texts[1].text = level.description;

            OfflineLevelConfig captured = level;
            btn.onClick.AddListener(() => OnLevelSelected(captured));
            _spawnedLevelButtons.Add(btn);
        }
    }

    private void OnLearningStyleSelected()
    {
        // Resetta il profilo ai default e aggiorna i bottoni prima di mostrare il pannello
        //_profile = new LearningProfileSelection();
        RefreshLearningStyleUI();

        ShowPanel(learningStylePanel);
    }

    // ── Pannello stile di apprendimento ───────────────────────────────────

    private void SetAttivoRiflessivo(LearningEnums.AttivoRiflessivo value)
    {
        _profile.attivoRiflessivo = value;
        RefreshPair(attivoButton, riflessivoButton,
            value == LearningEnums.AttivoRiflessivo.Attivo);
    }

    private void SetSensitivoIntuitivo(LearningEnums.SensitivoIntuitivo value)
    {
        _profile.sensitivoIntuitivo = value;
        RefreshPair(sensitivoButton, intuitivoButton,
            value == LearningEnums.SensitivoIntuitivo.Sensitivo);
    }

    private void SetVisivoVerbale(LearningEnums.VisivoVerbale value)
    {
        _profile.visivoVerbale = value;
        RefreshPair(visivoButton, verbaleButton,
            value == LearningEnums.VisivoVerbale.Visivo);
    }

    private void SetSequenzialeGlobale(LearningEnums.SequenzialeGlobale value)
    {
        _profile.sequenzialeGlobale = value;
        RefreshPair(sequenzialeButton, globaleButton,
            value == LearningEnums.SequenzialeGlobale.Sequenziale);
    }

    /// <summary>Colora il bottone selezionato e deseleziona l'altro.</summary>
    private void RefreshPair(Button first, Button second, bool firstSelected)
    {
        SetButtonColor(first, firstSelected ? selectedColor : unselectedColor);
        SetButtonColor(second, firstSelected ? unselectedColor : selectedColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        btn.colors = colors;
    }

    /// <summary>Aggiorna tutti i bottoni di stile in base al profilo corrente.</summary>
    private void RefreshLearningStyleUI()
    {
        RefreshPair(attivoButton, riflessivoButton,
            _profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo);
        RefreshPair(sensitivoButton, intuitivoButton,
            _profile.sensitivoIntuitivo == LearningEnums.SensitivoIntuitivo.Sensitivo);
        RefreshPair(visivoButton, verbaleButton,
            _profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo);
        RefreshPair(sequenzialeButton, globaleButton,
            _profile.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Sequenziale);
    }

    private void OnLevelSelected(OfflineLevelConfig level)
    {
        Debug.Log($"[MenuUI] Livello selezionato: {level.levelName}");
        _pendingLevel = level;

        if (_pendingLevel == null)
        {
            Debug.LogWarning("[MenuUI] Nessun livello selezionato!");
            return;
        }

        Debug.Log($"[MenuUI] Avvio livello '{_pendingLevel.levelName}' " +
                  $"| {_profile.attivoRiflessivo} | {_profile.sensitivoIntuitivo} " +
                  $"| {_profile.visivoVerbale} | {_profile.sequenzialeGlobale}");

        SessionManager.Instance.SetLearningProfile( _profile );
        SessionManager.Instance.StartOfflineSession(_pendingLevel);
    }

    // ── Input da tastiera (debug) ─────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1)) OnNewSessionClicked();
        if (Input.GetKeyDown(KeyCode.Keypad2)) OnContinueClicked();
        if (Input.GetKeyDown(KeyCode.Keypad3)) ShowPanel(levelsPanel);
    }
}