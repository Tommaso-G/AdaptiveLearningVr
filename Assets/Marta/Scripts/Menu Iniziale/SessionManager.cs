using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [SerializeField] private string gameSceneName = "ScenaUfficiale";
    [SerializeField] private string menuSceneName = "MenuScene";

    private string _activeSessionId;
    private bool _isNewSession;
    private Scene _gameScene;
    private bool _gameSceneLoaded = false;

    // ── Offline ───────────────────────────────────────────────────────────
    public OfflineLevelConfig SelectedOfflineLevel { get; private set; }
    public bool IsOffline => SelectedOfflineLevel != null;

    // ── Learning Profile ──────────────────────────────────────────────────
    /// <summary>
    /// Profilo scelto nel menu. Viene applicato a LearningProfile in scena all'avvio.
    /// Valido sia per sessioni offline che online.
    /// </summary>
    public LearningProfileSelection SelectedLearningProfile { get; private set; }
        = new LearningProfileSelection();

    public string UserPrefix { get; private set; } = "";
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SessionManager] Inizializzato");
    }

    public void SetLearningProfile(LearningProfileSelection profile)
    {
        SelectedLearningProfile = profile ?? new LearningProfileSelection();
    }

    public void SetUserPrefix(string prefix)
    {
        UserPrefix = prefix;
    }

    public void SetActiveSessionId(string id) => _activeSessionId = id;
    // ── Sessioni online ───────────────────────────────────────────────────

    public void StartNewSession()
    {
        Debug.Log("[SessionManager] Avvio nuova sessione");
        SelectedOfflineLevel = null;
        SessionPersistence.Clear();
        SessionPersistence.SetResetAll(true);
        _isNewSession = true;
        LoadGameScene();
    }

    public void ContinueSession()
    {
        if (!SessionPersistence.HasSavedSession())
        {
            Debug.LogWarning("[SessionManager] Nessuna sessione salvata!");
            return;
        }

        Debug.Log("[SessionManager] Continuazione sessione");
        SelectedOfflineLevel = null;
        SessionPersistence.SetResetAll(false);
        _activeSessionId = SessionPersistence.Load();
        _isNewSession = false;
        LoadGameScene();
    }

    // ── Sessioni offline ──────────────────────────────────────────────────

    /// <summary>
    /// Chiamato da MenuUI dopo che l'utente ha scelto livello e stile di apprendimento.
    /// </summary>
    public void StartOfflineSession(OfflineLevelConfig level)
    {
        if (level == null)
        {
            Debug.LogError("[SessionManager] StartOfflineSession: level è null!");
            return;
        }

        Debug.Log($"[SessionManager] Avvio sessione offline: {level.levelName}");
        SelectedOfflineLevel = level;
        _isNewSession = true;
        LoadGameScene();
    }

    // ── Navigazione ───────────────────────────────────────────────────────

    public void BackToMenu()
    {
        Debug.Log("[SessionManager] Ritorno al menu");
        SelectedOfflineLevel = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }

    public bool IsNewSession() => _isNewSession;
    public string GetActiveSessionId() => _activeSessionId;

    private void LoadGameScene()
    {
        if (_gameSceneLoaded && _gameScene.isLoaded)
        {
            Debug.Log("[SessionManager] ScenaUfficiale già caricata");
            SceneManager.SetActiveScene(_gameScene);
            return;
        }

        Debug.Log($"[SessionManager] Caricamento {gameSceneName}");
        StartCoroutine(LoadGameSceneAsync());
    }

    private IEnumerator LoadGameSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
            gameSceneName, LoadSceneMode.Single);

        yield return new WaitUntil(() => asyncLoad.isDone);

        _gameScene = SceneManager.GetSceneByName(gameSceneName);
        _gameSceneLoaded = true;
        SceneManager.SetActiveScene(_gameScene);

        Debug.Log($"[SessionManager] {gameSceneName} caricata e attivata");
    }
}

// ── Dati profilo di apprendimento trasportati tra le scene ────────────────

/// <summary>
/// Semplice contenitore serializzabile con le scelte fatte nel menu.
/// SessionManager lo porta in scena; LearningProfile lo legge in Awake.
/// </summary>
[System.Serializable]
public class LearningProfileSelection
{
    public LearningEnums.AttivoRiflessivo attivoRiflessivo = LearningEnums.AttivoRiflessivo.Attivo;
    public LearningEnums.SensitivoIntuitivo sensitivoIntuitivo = LearningEnums.SensitivoIntuitivo.Sensitivo;
    public LearningEnums.VisivoVerbale visivoVerbale = LearningEnums.VisivoVerbale.Visivo;
    public LearningEnums.SequenzialeGlobale sequenzialeGlobale = LearningEnums.SequenzialeGlobale.Sequenziale;
}