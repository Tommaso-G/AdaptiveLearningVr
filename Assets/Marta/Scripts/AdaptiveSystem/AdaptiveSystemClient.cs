// AdaptiveSystemClient.cs
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ── Strutture dati per serializzazione JSON ───────────────────────────────

[Serializable]
public class StartSessionRequest
{
    public string session_id;
    public ChapterConfigData[] chapters;
}

[Serializable]
public class ChapterConfigData
{
    public string chapter_id;
    public string name;
    public bool is_mandatory;
    public float weight = 1.0f;
    public int max_iterations = 5;
}

[Serializable]
public class ObserveRequest
{
    public string session_id;
    public string chapter_id;
    public string chapter_name;
    public int errors;
    public float time_sec;
}

[Serializable]
public class DecisionResponse
{
    public string chapter_id;
    public string skill_label;
    public float posterior_expert;
    public float posterior_intermediate;
    public float posterior_novice;
    public int feedback_level;       // 0=nessuno, 1=highlight, 2=istruzioni
    public bool feedback_changed;
    public bool add_optional;
    public string added_chapter_id; // null se nessun capitolo è aggiunto
    public string removed_chapter_id;  // null se nessun capitolo rimosso
    public bool remove_optional;
    public bool chapter_mastered;
    public string[] active_chapters;
    public string message;
}

[Serializable]
public class StartSessionResponse
{
    public string status;
    public string session_id;
    public string[] active_chapters;
}

[Serializable]
public class ChapterDetail
{
    public string chapter_id;
    public bool is_active;
    public int feedback_level;
}

[Serializable]
public class SessionStateResponse
{
    public string session_id;
    public string[] active_chapters;
    public ChapterDetail[] chapter_details;
    public bool is_complete;
}


// ── Client principale ────────────────────────────────────────────────────

public class AdaptiveSystemClient : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://localhost:8000";

    // Genera un ID sessione unico per ogni run
    private string _sessionId;

    // Evento che altri script possono ascoltare per reagire alle decisioni
    public static event Action<DecisionResponse> OnDecisionReceived;
    public static event Action<string[]> OnSessionStarted;

    // Singleton semplice per accesso globale
    public static AdaptiveSystemClient Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Genera un ID sessione unico basato sul timestamp
        _sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    // ── Avvia una nuova sessione ──────────────────────────────────────────

    /// <summary>
    /// Chiama questo metodo all'avvio dell'esperienza, passando la lista
    /// dei capitoli definiti in VRBuilder.
    /// </summary>
    public void StartSession(ChapterConfigData[] chapters, Action<string[]> callback = null)
    {
        StartCoroutine(StartSessionCoroutine(chapters, callback));
    }

    private IEnumerator StartSessionCoroutine(ChapterConfigData[] chapters, Action<string[]> callback = null)
    {
        var requestData = new StartSessionRequest
        {
            session_id = _sessionId,
            chapters = chapters
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"[AdaptiveSystem] Avvio sessione: {json}");

        using var request = new UnityWebRequest($"{serverUrl}/session/start", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<StartSessionResponse>(
                request.downloadHandler.text
            );
            Debug.Log($"[AdaptiveSystem] Sessione avviata. " +
                      $"Capitoli attivi: {string.Join(", ", response.active_chapters)}");
            OnSessionStarted?.Invoke(response.active_chapters);
            callback?.Invoke(response.active_chapters);
        }
        else
        {
            Debug.LogError($"[AdaptiveSystem] Errore avvio sessione: {request.error}");
        }
    }

    // ── Recupera dati di inizilizzazione per la nuova sessione ──────────────────────────────────────────

    /// <summary>
    /// Recupera lo stato corrente della sessione dal server.
    /// Chiamato all'inizio di ogni run dopo la prima.
    /// </summary>
    public void RestoreSession(string sessionId,
                               Action<SessionStateResponse> callback = null)
    {
        _sessionId = sessionId;
        StartCoroutine(RestoreSessionCoroutine(sessionId, callback));
    }

    private IEnumerator RestoreSessionCoroutine(string sessionId,
                                                 Action<SessionStateResponse> callback)
    {
        using var request = UnityWebRequest.Get(
            $"{serverUrl}/session/{sessionId}/state"
        );
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var state = JsonUtility.FromJson<SessionStateResponse>(
                request.downloadHandler.text
            );
            Debug.Log($"[AdaptiveSystem] Stato sessione recuperato. " +
                      $"Capitoli attivi: {string.Join(", ", state.active_chapters)}");
            //OnSessionStateRestored?.Invoke(state);
            callback?.Invoke(state);
        }
        else
        {
            Debug.LogWarning($"[AdaptiveSystem] Impossibile recuperare la sessione " +
                             $"({request.error}). Avvio nuova sessione.");
            callback?.Invoke(null);
        }
    }

    // ── Invia osservazione per un capitolo ────────────────────────────────

    /// <summary>
    /// Chiama questo metodo quando l'utente completa un capitolo VRBuilder.
    /// errors: numero di errori commessi nel capitolo
    /// timeSec: tempo in minuti (lettura istruzioni + esecuzione)
    /// callback: chiamato con la decisione quando il server risponde
    /// </summary>
    public void SendObservation(
        string chapterId,
        string chapterName,
        int errors,
        float timeSec,
        Action<DecisionResponse> callback = null)
    {
        StartCoroutine(ObserveCoroutine(chapterId, chapterName, errors, timeSec, callback));
    }

    private IEnumerator ObserveCoroutine(
        string chapterId,
        string chapterName,
        int errors,
        float timeSec,
        Action<DecisionResponse> callback)
    {
        var requestData = new ObserveRequest
        {
            session_id = _sessionId,
            chapter_id = chapterId,
            chapter_name = chapterName,
            errors = errors,
            time_sec = timeSec
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"[AdaptiveSystem] Osservazione: {chapterName} " +
                  $"errori={errors} tempo={timeSec:F1}sec");

        using var request = new UnityWebRequest($"{serverUrl}/observe", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<DecisionResponse>(
                request.downloadHandler.text
            );

            Debug.Log($"[AdaptiveSystem] Decisione ricevuta: " +
                      $"skill={response.skill_label} " +
                      $"feedback={response.feedback_level} " +
                      $"add_optional={response.add_optional} " +
                      $"remove_optional={response.remove_optional}");

            // Notifica tutti i listener tramite evento statico
            OnDecisionReceived?.Invoke(response);

            // Chiama anche il callback specifico se fornito
            callback?.Invoke(response);
        }
        else
        {
            Debug.LogError($"[AdaptiveSystem] Errore observe: " +
                           $"{request.error}\n{request.downloadHandler.text}");
        }
    }

    public string GetSessionId() => _sessionId;
}