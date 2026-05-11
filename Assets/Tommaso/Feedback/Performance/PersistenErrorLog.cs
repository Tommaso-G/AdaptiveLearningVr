using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Oggetto persistente tra le scene che mantiene il log degli errori.
/// Collega textPanelOnStart direttamente qui nell'Inspector.
/// </summary>
public class PersistentErrorLog : MonoBehaviour
{
    public static PersistentErrorLog Instance { get; private set; }

    [Header("UI")]
    public TMP_Text textPanelOnStart;
    public RectTransform parentCanvas;

    [Header("Reset")]
    [Tooltip("Se true, azzera il log al prossimo RegisterEntry. Torna automaticamente a false dopo il reset.")]
    public bool resetOnNext = false;

    // Profilo salvato dell'iterazione precedente (Sequenziale o Globale come stringa)
    private string _lastProfile = "";

    // Contenuto salvato del pannello
    private string _savedText = "";

    // Flag interno: il primo errore di questa sessione è già arrivato?
    private bool _sessionStarted = false;

    // ─────────────────────────────────────────────────────────────────

    private const string KEY_TEXT = "PersistentErrorLog_Text";
    private const string KEY_PROFILE = "PersistentErrorLog_Profile";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ripristina da PlayerPrefs
        _savedText = PlayerPrefs.GetString(KEY_TEXT, "");
        _lastProfile = PlayerPrefs.GetString(KEY_PROFILE, "");

        if (textPanelOnStart != null && !string.IsNullOrEmpty(_savedText))
            textPanelOnStart.text = _savedText;
    }

    // ─────────────────────────────────────────────────────────────────
    // API PUBBLICA
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chiamato da StepErrorTracker all'inizio di ogni scena con il profilo corrente.
    /// Se il profilo è cambiato rispetto all'iterazione precedente, azzera tutto.
    /// </summary>
    public void InitSession(string currentProfile)
    {
        _sessionStarted = false;

        bool profileChanged = !string.IsNullOrEmpty(_lastProfile) && _lastProfile != currentProfile;

        if (resetOnNext || profileChanged)
        {
            _savedText = "";
            _lastProfile = currentProfile;
            resetOnNext = false;

            PlayerPrefs.SetString(KEY_TEXT, "");
            PlayerPrefs.SetString(KEY_PROFILE, currentProfile);
            PlayerPrefs.Save();

            if (textPanelOnStart != null)
                textPanelOnStart.text = "";

            Debug.Log($"[PersistentErrorLog] Log azzerato. Motivo: {(profileChanged ? "profilo cambiato" : "reset manuale")}");
        }
        else
        {
            _lastProfile = currentProfile;
            PlayerPrefs.SetString(KEY_PROFILE, currentProfile);
            PlayerPrefs.Save();

            if (textPanelOnStart != null && !string.IsNullOrEmpty(_savedText))
                textPanelOnStart.text = _savedText;
        }

    }

    /// <summary>
    /// Aggiorna il pannello con il nuovo testo.
    /// Al primo errore della sessione sovrascrive il testo precedente.
    /// </summary>
    public void UpdateText(string newText)
    {
        if (!_sessionStarted)
            _sessionStarted = true;

        _savedText = newText;
        PlayerPrefs.SetString(KEY_TEXT, _savedText);
        PlayerPrefs.Save();

        if (textPanelOnStart != null)
        {
            textPanelOnStart.text = _savedText;
            if (parentCanvas != null)
                StartCoroutine(ResetCanvasNextFrames());
        }
    }

    /// <summary>
    /// Azzera il log immediatamente.
    /// </summary>
    private System.Collections.IEnumerator ResetCanvasNextFrames()
    {
        yield return null;
        yield return null;
        yield return null;
        parentCanvas.gameObject.SetActive(false);
        yield return null;
        parentCanvas.gameObject.SetActive(true);
    }

    public void ResetLog()
    {
        _savedText = "";
        _sessionStarted = false;

        PlayerPrefs.SetString(KEY_TEXT, "");
        PlayerPrefs.Save();

        if (textPanelOnStart != null)
            textPanelOnStart.text = "";

        Debug.Log("[PersistentErrorLog] Log azzerato manualmente.");
    }
}