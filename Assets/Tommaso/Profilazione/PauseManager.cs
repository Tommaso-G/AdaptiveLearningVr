using UnityEngine;
using UnityEngine.Events;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool IsPaused { get; private set; } = false;

    [Header("Events")]
    public UnityEvent OnPause;
    public UnityEvent OnResume;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        OnPause?.Invoke();
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        OnResume?.Invoke();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }
}