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
        SessionPersistence.SetResetAll(false);
        _activeSessionId = SessionPersistence.Load();
        _isNewSession = false;
        LoadGameScene();
    }

    public void BackToMenu()
    {
        Debug.Log("[SessionManager] Ritorno al menu");
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }

    public bool IsNewSession() => _isNewSession;

    public string GetActiveSessionId() => _activeSessionId;

    private void LoadGameScene()
    {
        // Se la scena è già caricata, non ricaricarla
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
        // Carica additivamente
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
            gameSceneName,
            LoadSceneMode.Single
        );

        yield return new WaitUntil(() => asyncLoad.isDone);

        // Recupera la scena caricata
        _gameScene = SceneManager.GetSceneByName(gameSceneName);
        _gameSceneLoaded = true;

        // Attivala come scena principale
        SceneManager.SetActiveScene(_gameScene);

        Debug.Log($"[SessionManager] {gameSceneName} caricata e attivata");
    }
}