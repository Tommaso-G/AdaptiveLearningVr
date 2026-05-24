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
    /// <summary>Livello offline scelto nel menu. Null = modalità online.</summary>
    public OfflineLevelConfig SelectedOfflineLevel { get; private set; }

    /// <summary>True se la sessione corrente è offline.</summary>
    public bool IsOffline => SelectedOfflineLevel != null;

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

    /// <summary>
    /// Avvia il gioco in modalità offline con il livello specificato.
    /// Chiamato dal MenuUI quando l'utente seleziona un livello offline.
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
            gameSceneName,
            LoadSceneMode.Single
        );

        yield return new WaitUntil(() => asyncLoad.isDone);

        _gameScene = SceneManager.GetSceneByName(gameSceneName);
        _gameSceneLoaded = true;

        SceneManager.SetActiveScene(_gameScene);

        Debug.Log($"[SessionManager] {gameSceneName} caricata e attivata");
    }
}