// MenuUI.cs (nella scena MenuScene)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuUI : MonoBehaviour
{
    [Header("Bottoni principali")]
    [SerializeField] private Button newSessionButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button offlineModeButton;   // NEW: apre il pannello livelli

    [Header("Testi informativi")]
    [SerializeField] private TextMeshProUGUI sessionInfoText;
    [SerializeField] private TextMeshProUGUI instructionsText;

    [Header("Pannello livelli offline")]
    [Tooltip("GameObject contenitore dei bottoni livello (inizialmente disattivato)")]
    [SerializeField] private GameObject offlineLevelsPanel;

    [Tooltip("Prefab di un bottone livello. Deve avere un componente Button e un figlio TMP_Text.")]
    [SerializeField] private Button levelButtonPrefab;

    [Tooltip("Transform padre in cui istanziare i bottoni livello (es. un VerticalLayoutGroup).")]
    [SerializeField] private Transform levelButtonContainer;

    [Header("Configurazione livelli offline")]
    [SerializeField] private OfflineLevelRegistry offlineLevelRegistry;

    // Bottoni istanziati dinamicamente
    private readonly List<Button> _spawnedButtons = new List<Button>();

    private void Start()
    {
        Outline[] outlines = FindObjectsByType<Outline>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var outline in outlines)
        {
            outline.enabled = false;
            Debug.Log($"[MenuUI] Outline disabilitato su {outline.gameObject.name}");
        }

        // Listener bottoni principali
        newSessionButton.onClick.AddListener(OnNewSessionClicked);
        continueButton.onClick.AddListener(OnContinueClicked);

        if (offlineModeButton != null)
            offlineModeButton.onClick.AddListener(OnOfflineModeClicked);

        // Stato sessione salvata
        bool hasSavedSession = SessionPersistence.HasSavedSession();
        continueButton.interactable = hasSavedSession;
        sessionInfoText.text = hasSavedSession
            ? $"Sessione salvata: {SessionPersistence.Load()}"
            : "Nessuna sessione salvata";

        instructionsText.text = "Scegli un'opzione per iniziare:";

        // Pannello offline: chiuso all'avvio
        if (offlineLevelsPanel != null)
            offlineLevelsPanel.SetActive(false);

        // Genera i bottoni livello in anticipo (ma il pannello è nascosto)
        BuildLevelButtons();
    }

    // ── Bottoni principali ────────────────────────────────────────────────

    private void OnNewSessionClicked()
    {
        Debug.Log("[MenuUI] Nuova sessione avviata");
        HideOfflineLevels();
        SessionManager.Instance.StartNewSession();
    }

    private void OnContinueClicked()
    {
        if (SessionPersistence.HasSavedSession())
        {
            Debug.Log("[MenuUI] Continuazione sessione");
            HideOfflineLevels();
            SessionManager.Instance.ContinueSession();
        }
        else
        {
            Debug.LogWarning("[MenuUI] Nessuna sessione da continuare!");
        }
    }

    private void OnOfflineModeClicked()
    {
        if (offlineLevelsPanel == null) return;

        bool isNowVisible = !offlineLevelsPanel.activeSelf;
        offlineLevelsPanel.SetActive(isNowVisible);

        instructionsText.text = isNowVisible
            ? "Seleziona un livello offline:"
            : "Scegli un'opzione per iniziare:";
    }

    // ── Livelli offline ───────────────────────────────────────────────────

    private void BuildLevelButtons()
    {
        if (offlineLevelRegistry == null)
        {
            Debug.LogWarning("[MenuUI] OfflineLevelRegistry non assegnato nell'Inspector!");
            return;
        }
        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogWarning("[MenuUI] levelButtonPrefab o levelButtonContainer non assegnati!");
            return;
        }

        // Ripulisce eventuali bottoni vecchi (utile in Editor)
        foreach (var b in _spawnedButtons)
            if (b != null) Destroy(b.gameObject);
        _spawnedButtons.Clear();

        foreach (OfflineLevelConfig level in offlineLevelRegistry.levels)
        {
            if (level == null) continue;

            Button btn = Instantiate(levelButtonPrefab, levelButtonContainer);

            // Imposta testo del bottone
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = level.levelName;

            // Tooltip/descrizione opzionale: cercalo come componente TextMeshProUGUI
            // separato con tag "Description" se presente nel prefab
            var texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
                texts[1].text = level.description;

            // Cattura la variabile locale per la lambda
            OfflineLevelConfig capturedLevel = level;
            btn.onClick.AddListener(() => OnLevelSelected(capturedLevel));

            _spawnedButtons.Add(btn);
        }
    }

    private void OnLevelSelected(OfflineLevelConfig level)
    {
        Debug.Log($"[MenuUI] Livello offline selezionato: {level.levelName}");
        HideOfflineLevels();
        SessionManager.Instance.StartOfflineSession(level);
    }

    private void HideOfflineLevels()
    {
        if (offlineLevelsPanel != null)
            offlineLevelsPanel.SetActive(false);

        instructionsText.text = "Scegli un'opzione per iniziare:";
    }

    // ── Input da tastiera (debug) ─────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
            OnNewSessionClicked();

        if (Input.GetKeyDown(KeyCode.Keypad2))
            OnContinueClicked();

        if (Input.GetKeyDown(KeyCode.Keypad3))
            OnOfflineModeClicked();
    }
}