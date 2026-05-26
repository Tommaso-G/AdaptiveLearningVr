using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRBuilder.Core;
using System.Linq;
using VRBuilder.Core.Behaviors;
using UnityEngine.UI;
using static UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile;
using System.IO;

public class GameManager : MonoBehaviour
{
    [Header("Chapter Configuration")]
    public List<ChapterConfigData> allChapters;
    // Assegna nell'Inspector la lista completa di tutti i capitoli,
    // obbligatori e opzionali, nell'ordine in cui vuoi aggiungerli.
    public List<ChapterConfigData> excludedChapters;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "ScenaUfficiale";
    [SerializeField] private ChaptersOrderManager chaptersOrderMgr = null;
    [SerializeField] private ChapterTracker chapterTracker = null;
    [SerializeField] private StepErrorTracker stepErrorTracker;
    [SerializeField] private FeedbackChapterFilter feedbackChapterFilter = null;
    [SerializeField] private DifficultyChapterFilter difficultyChapterFilter = null;

    //[Header("Iteration Control")]
    //[SerializeField] private Button endIterationButton = null;

    //[Header("Session Control")]
    //[SerializeField] public bool resetAll = false;

    // offlineMode è ora derivato dal SessionManager: true quando è stato scelto un livello offline dal menu.
    private bool offlineMode => SessionManager.Instance != null && SessionManager.Instance.IsOffline;

    private IProcess process;
    private Dictionary<string, string> chaptersIdToName = new Dictionary<string, string>();

    /// <summary>ID sessione generato localmente quando offlineMode = true.</summary>
    private string _offlineSessionId;

    private int currentIterationNumber = 1;
    private List<string> chaptersCompletedThisIteration = new List<string>();
    private List<string> active_chapters = new List<string>();
    private bool _endIterationPending = false;

    private class ChapterRuntimeData
    {
        public string chapterId;
        public string chapterName;
        public float time;
        public List<StepError> errors = new();
    }

    private Dictionary<string, ChapterRuntimeData> chapterData = new();

    private void Start()
    {
        chaptersOrderMgr = FindFirstObjectByType<ChaptersOrderManager>();
        chapterTracker = FindFirstObjectByType<ChapterTracker>();
        feedbackChapterFilter = FindFirstObjectByType<FeedbackChapterFilter>();
        difficultyChapterFilter = FindFirstObjectByType<DifficultyChapterFilter>();
        stepErrorTracker = FindFirstObjectByType<StepErrorTracker>();

        if (!offlineMode)
        {
            AdaptiveSystemClient.OnDecisionReceived += HandleDecision;
            AdaptiveSystemClient.OnSessionStarted += SaveActiveChapters;
        }
        chapterTracker.ObservationDataReady += SendObservation;

        //if (resetAll)
        //{
        //    Debug.Log("[GameManager] resetAll=true: Eliminando sessione precedente");
        //    SessionPersistence.Clear();
        //}

        bool isNewSession = SessionManager.Instance.IsNewSession();

        if (isNewSession)
        {
            Debug.Log("[GameManager] Nuova sessione (resetAll=true)");
        }
        else
        {
            Debug.Log("[GameManager] Continuazione sessione (resetAll=false)");
        }

        //if (endIterationButton != null)
        //{
        //    endIterationButton.onClick.AddListener(OnEndIterationButtonPressed);
        //}

        chapterTracker.setChaptersToExclude(excludedChapters);
        // Registriamo l'evento di VRBuilder
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
        StartCoroutine(InitializeSession());
    }

    private IEnumerator InitializeSession()
    {
        if (offlineMode)
        {
            yield return StartCoroutine(StartOfflineSession());
            yield break;
        }

        bool isNewSession = SessionManager.Instance.IsNewSession();

        if (isNewSession || !SessionPersistence.HasSavedSession())
        {
            Debug.Log("[GameManager] Avviando nuova sessione");
            yield return StartCoroutine(StartNewSession());
        }
        else
        {
            Debug.Log("[GameManager] Riprendendo sessione esistente");
            string sessionId = SessionManager.Instance.GetActiveSessionId();
            yield return StartCoroutine(RestoreAndInitialize(sessionId));
        }
    }

    // ============================
    // OFFLINE MODE
    // ============================

    /// <summary>
    /// Inizializza una sessione puramente locale, senza chiamate di rete.
    /// Popola chaptersIdToName e active_chapters da allChapters,
    /// poi lascia VRBuilder procedere normalmente.
    /// </summary>
    private IEnumerator StartOfflineSession()
    {
        _offlineSessionId = $"OFFLINE_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        currentIterationNumber = 1;
        chaptersCompletedThisIteration.Clear();
        chapterData.Clear();

        if (allChapters == null || allChapters.Count == 0)
        {
            Debug.LogWarning("[GameManager][OFFLINE] allChapters e' vuota: nessun capitolo configurato.");
            yield break;
        }

        // Recupera il livello scelto nel menu
        OfflineLevelConfig level = SessionManager.Instance.SelectedOfflineLevel;
        if (level == null)
        {
            Debug.LogWarning("[GameManager][OFFLINE] Nessun livello offline selezionato, uso default.");
        }
        else
        {
            Debug.Log($"[GameManager][OFFLINE] Livello selezionato: {level.levelName}");
        }

        // Popola mappa ID-nome
        chaptersIdToName.Clear();
        active_chapters.Clear();

        foreach (var chapter in allChapters)
        {
            chaptersIdToName[chapter.chapter_id] = chapter.name;

            // I capitoli opzionali vengono aggiunti solo se presenti nel livello
            bool isOptional = chapter.name.Contains("Optional");
            if (!isOptional)
            {
                active_chapters.Add(chapter.chapter_id);
            }
        }

        // Capitoli opzionali da aggiungere secondo il livello
        if (level != null && level.optionalChaptersToAdd != null)
        {
            List<string> idsToAdd = new List<string>();
            foreach (var entry in level.optionalChaptersToAdd)
            {
                if (entry == null) continue;
                foreach (var chapter in allChapters)
                {
                    if (chapter.name == entry.chapterName)
                    {
                        active_chapters.Add(chapter.chapter_id);
                        idsToAdd.Add(chapter.chapter_id);
                        break;
                    }
                }
            }
            StartCoroutine(AddOptionalChapter(idsToAdd));
        }

        // Feedback per capitolo
        if (feedbackChapterFilter != null)
        {
            // Default: feedback completo (0) per tutti
            foreach (var setting in feedbackChapterFilter.chapterSettings)
                setting.feedbackLevel = 0;

            // Override dal livello
            if (level != null && level.feedbackOverrides != null)
            {
                foreach (var ovr in level.feedbackOverrides)
                    feedbackChapterFilter.setFeedbackLevel(ovr.chapterName, ovr.feedbackLevel);
            }
        }

        // Difficolta' per capitolo
        if (difficultyChapterFilter != null && level != null && level.difficultyOverrides != null)
        {
            foreach (var ovr in level.difficultyOverrides)
                difficultyChapterFilter.SetDifficultyLevel(ovr.chapterName, ovr.difficultyLevel);
        }

        string msg = "[GameManager][OFFLINE] Capitoli attivi:\n";
        foreach (var id in active_chapters)
            msg += $"  - {chaptersIdToName[id]}\n";
        Debug.Log(msg);

        Debug.Log($"[GameManager][OFFLINE] Sessione offline avviata. ID locale: {_offlineSessionId}");

        // Pannello progressi: livello 1 parte pulito, livelli successivi mostrano errori precedenti.
        if (stepErrorTracker != null && stepErrorTracker.errorLog != null)
        {
            if (level != null && level.isFirstLevel)
            {
                stepErrorTracker.errorLog.resetOnNext = true;
            }
            else
            {
                stepErrorTracker.errorLog.resetOnNext = false;
            }

                string profileKey = "Offline";
            stepErrorTracker.errorLog.InitSession(profileKey);
        }

        yield break;
    }

    private IEnumerator EndIterationOffline()
    {
        _endIterationPending = false;

        string json = BuildIterationJsonOffline();
        SaveIterationJsonToFileOffline(json);

        Debug.Log($"[GameManager][OFFLINE] Fine iterazione {currentIterationNumber} salvata localmente.");
        currentIterationNumber++;

        SessionManager.Instance.BackToMenu();
        yield break;
    }

    private string BuildIterationJsonOffline()
    {
        SessionJson session = new SessionJson();
        session.session_id = _offlineSessionId;
        session.iteration = currentIterationNumber;

        foreach (var kvp in chapterData)
        {
            var data = kvp.Value;
            stepErrorTracker.ChapterErrors.TryGetValue(data.chapterName, out var chapterErrorData);

            session.chapters.Add(new ChapterJson
            {
                chapter_id = data.chapterId,
                chapter_name = data.chapterName,
                time_seconds = data.time,
                errors = chapterErrorData != null ? chapterErrorData.errors : new List<StepError>()
            });
        }

        return JsonUtility.ToJson(session, true);
    }

    private void SaveIterationJsonToFileOffline(string json)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Sessions", _offlineSessionId);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"iter_{currentIterationNumber}.json");
        File.WriteAllText(path, json);
        Debug.Log("[GameManager][OFFLINE] Salvato in: " + path);
    }

    private IEnumerator StartNewSession()
    {
        bool done = false;

        if (allChapters != null)
        {

            foreach (var chapter in allChapters)
            {
                chaptersIdToName[chapter.chapter_id] = chapter.name;
            }

            AdaptiveSystemClient.Instance.StartSession(
                    allChapters.ToArray(),
                    true,
                    activeChapters =>
                    {
                        // Salva solo il session_id
                        SessionPersistence.Save(
                            AdaptiveSystemClient.Instance.GetSessionId()
                        );

                        // Resetta i dati dell'iterazione corrente
                        currentIterationNumber = 1;
                        chaptersCompletedThisIteration.Clear();

                        // feedback massimo per tutti alla prima run
                        foreach (var chapter in feedbackChapterFilter.chapterSettings)
                        {
                            chapter.feedbackLevel = 0;
                        }

                        List<string> chapterToAdd = new List<string>();
                        foreach (string ac in activeChapters)
                        {
                            if (chaptersIdToName[ac].Contains("Optional"))
                            {
                                chapterToAdd.Add(ac);
                            }
                        }

                        StartCoroutine(AddOptionalChapter(chapterToAdd));

                        done = true;
                    }
                );
        }
        else
        {
            print("[GameManager] Lista all chapters vuota");
        }

        yield return new WaitUntil(() => done);
    }

    private IEnumerator RestoreAndInitialize(string sessionId)
    {
        bool done = false;
        AdaptiveSystemClient.Instance.RestoreSession(
            sessionId,
            false,
            state => {
                if (state == null)
                {
                    // Server non raggiungibile: non dovrebbe succedere
                    // se il server ÃÂ¨ sempre attivo, ma gestiamo il caso
                    Debug.LogError("[GameManager] Server non raggiungibile.");
                    done = true;
                    return;
                }

                if (state.is_complete)
                {
                    //UIManager.Instance.ShowCompletionScreen();
                    done = true;
                    return;
                }

                currentIterationNumber = state.iteration_number;
                chaptersCompletedThisIteration.Clear();
                Debug.Log($"[GameManager] Iterazione {currentIterationNumber} ripresa");

                if (allChapters.Count == 0)
                {
                    for (int i = 0; i < process.Data.Chapters.Count; i++)
                    {
                        var c = process.Data.Chapters[i];
                        chaptersIdToName[c.ChapterMetadata.Guid.ToString()] = c.Data.Name;
                    }
                }
                else
                {
                    foreach (var chapter in allChapters)
                    {
                        chaptersIdToName[chapter.chapter_id] = chapter.name;
                    }
                }



                // Imposta il feedback e la difficoltÃÂ  per ogni capitolo
                foreach (var chapterData in state.chapter_details)
                {
                    if (chaptersIdToName.TryGetValue(chapterData.chapter_id, out string name))
                    {
                        feedbackChapterFilter.setFeedbackLevel(name, chapterData.feedback_level);
                    }

                    if (difficultyChapterFilter != null)
                    {
                        difficultyChapterFilter.SetDifficultyLevel(name, chapterData.difficulty_level);
                    }
                }

                List<string> chapterToAdd = new List<string>();
                foreach (string ac in state.active_chapters)
                {
                    if (chaptersIdToName[ac].Contains("Optional"))
                    {
                        chapterToAdd.Add(ac);
                    }
                }

                string message = "[GameManager] Prior per capitoli: \n";
                foreach (var chapter in state.chapter_details)
                {
                    message += $"- capitolo {chaptersIdToName[chapter.chapter_id]} -> prior: {string.Join(", ", chapter.chapter_prior)}\n";
                }

                print(message);

                AddOptionalChapter(chapterToAdd);

                AdaptiveSystemClient.Instance.StartSession(
                   allChapters.ToArray(),
                   false
               );
                done = true;
            }
        );
        yield return new WaitUntil(() => done);
    }

    private string BuildIterationJson()
    {
        SessionJson session = new SessionJson();

        session.session_id = SessionManager.Instance.GetActiveSessionId();
        session.iteration = currentIterationNumber;

        foreach (var kvp in chapterData) // tuo contenitore tempo+info capitolo
        {
            var data = kvp.Value;

            var errorData = stepErrorTracker.ChapterErrors.TryGetValue(
                data.chapterName,
                out var chapterErrorData
            );

            ChapterJson chapterJson = new ChapterJson
            {
                chapter_id = data.chapterId,
                chapter_name = data.chapterName,
                time_seconds = data.time,
                errors = chapterErrorData != null
                        ? chapterErrorData.errors
                        : new List<StepError>()
            };

            session.chapters.Add(chapterJson);
        }

        return JsonUtility.ToJson(session, true);
    }

    private void SaveIterationJsonToFile(string json)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Sessions", AdaptiveSystemClient.Instance.GetSessionId());
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"iter_{currentIterationNumber}.json");

        File.WriteAllText(path, json);

        Debug.Log("[GameManager] Salvato in: " + path);
    }

    public void OnEndIterationButtonPressedUI()
    {
        Debug.Log("[GameManager] Bottone UI: Fine iterazione");
        OnEndIterationButtonPressed();
    }
    private void OnEndIterationButtonPressed()
    {
        Debug.Log("[GameManager] Bottone End Iteration premuto");

        string message = "[GameManager] Capitoli attivi alla FINE dell'iterazione: \n";
        foreach (var chapter in active_chapters)
        {
            message += $"- Capitolo {chaptersIdToName[chapter]}\n";
        }

        print(message);

        if (_endIterationPending)
        {
            Debug.LogWarning("[GameManager] EndIteration giÃÂ  in corso, ignorato.");
            return;
        }
        _endIterationPending = true;

        if (offlineMode)
        {
            StartCoroutine(EndIterationOffline());
            return;
        }

        StartCoroutine(EndIterationCoroutine(active_chapters));
    }

    // ===== NUOVO: Chiama il server per marcare fine iterazione =====
    private IEnumerator EndIterationCoroutine(List<string> activeChapters)
    {
        string json = BuildIterationJson();
        SaveIterationJsonToFile(json);
        yield return StartCoroutine(
            AdaptiveSystemClient.Instance.EndIteration(
                activeChapters.ToArray(),
                result =>
                {
                    _endIterationPending = false;
                    if (result.status == "iteration_complete")
                    {
                        Debug.Log(
                            $"[GameManager] Ã¢ÂÂ Iterazione {result.iteration_number} COMPLETA! " +
                            $"Prossima: {result.next_iteration}"
                        );
                        currentIterationNumber = result.next_iteration;
                        // ===== NUOVO: Torna al menu dopo iterazione completa =====
                        SessionManager.Instance.BackToMenu();
                    }
                    else if (result.status == "iteration_incomplete")
                    {
                        Debug.LogWarning(
                            $"[GameManager] Ã¢ÂÂ Ã¯Â¸Â Iterazione INCOMPLETA! " +
                            $"Mancanti: {string.Join(", ", result.incomplete_chapters)}"

                        );
                        // Torna al menu dopo iterazione incompleta
                        SessionManager.Instance.BackToMenu();
                    }
                }
            )
        );
    }

    public void HandleDecision(DecisionResponse decision)
    {
        // Aggiorna il feedback visivo in base al livello deciso dalla BN
        //FeedbackManager.Instance.SetFeedbackLevel(chapterId, decision.feedback_level);
        if (decision.message != null)
        {
            print(decision.message);
        }

        // se cambiato il livello di feedback aggiorna
        if (decision.feedback_changed)
        {
            feedbackChapterFilter.setFeedbackLevel(chaptersIdToName[decision.chapter_id], decision.feedback_level);
        }

        if (decision.difficulty_changed)
        {
            // cambia la versione del capitolo se presente
        }

        if (decision.add_optional)
        {
            Debug.Log("[GameManager] Nuovo capitolo opzionale attivato");
        }


        // Se stato rimosso un capitolo opzionale
        if (decision.remove_optional)
        {
            chaptersOrderMgr.RemoveChapter(chapterToRemoveName: chaptersIdToName[decision.removed_chapter_id]);
            active_chapters.Remove(chaptersIdToName[decision.removed_chapter_id]);
            Debug.Log("[GameManager] Capitolo opzionale rimosso: " + chaptersIdToName[decision.removed_chapter_id]);
        }

        //// Se il capitolo padroneggiato, puoi mostrare un feedback positivo
        //if (decision.chapter_mastered)
        //{
        //    Debug.Log($"[ChapterTracker] capitolo padroneggiato!");
        //    //UIManager.Instance.ShowMasteryBadge(chapterId);
        //}
    }

    private void SaveActiveChapters(string[] response)
    {
        active_chapters.Clear();

        string message = "[GameManager] Capitoli attivi all'INIZIO dell'iterazione: \n";
        foreach (var chapter in response)
        {
            message += $"- Capitolo {chaptersIdToName[chapter]}\n";
            active_chapters.Add(chapter);
        }
        print(message);
    }

    private void Update()
    {
        // ===== MODIFICATO: Z per salvare fine iterazione =====
        if (Input.GetKeyUp(KeyCode.Z))
        {
            Debug.Log("[GameManager] Z: Fine iterazione");
            OnEndIterationButtonPressed();
        }

        // ===== NUOVO: ESC per tornare al menu =====
        //if (Input.GetKeyUp(KeyCode.Space))
        //{
        //    Debug.Log("[GameManager] SPACE: Ritorno al menu");
        //    SessionManager.Instance.BackToMenu();
        //}
    }

    private IEnumerator AddOptionalChapter(List<string> chapterToAdd)
    {
        while (!chaptersOrderMgr.EditorChaptersReady)
        {
            yield return null;
        }
            foreach (string id in chapterToAdd)
        {
            print("[GameManager] Capitolo da aggiungere: " + chaptersIdToName[id]);
            chaptersOrderMgr.AddOptional(chaptersIdToName[id]);
        }
    }

    private void SendObservation(string chapter_id, string chapter_name, int errors, float time)
    {
        if (!chapterData.ContainsKey(chapter_id))
        {
            chapterData[chapter_id] = new ChapterRuntimeData
            {
                chapterId = chapter_id,
                chapterName = chapter_name
            };
        }

        chapterData[chapter_id].time = time;

        if (offlineMode)
        {
            // In modalità offline registriamo solo i dati locali, niente rete.
            Debug.Log($"[GameManager][OFFLINE] Osservazione registrata localmente per '{chapter_name}': errori={errors}, tempo={time:F1}s");

            // Aggiorna il PersistentErrorLog con gli errori del capitolo appena concluso,
            // ma solo se non e' il primo capitolo attivo (per il primo non c'e' un precedente).
            if (stepErrorTracker != null)
            {
                bool isFirstChapter = active_chapters.Count > 0
                    && active_chapters[0] == chapter_id;

                if (!isFirstChapter)
                    stepErrorTracker.NotifyChapterCompleted(chapter_name);
            }

            return;
        }

        // Invia al sistema adattivo e gestisci la risposta
        AdaptiveSystemClient.Instance.SendObservation(
            chapterId: chapter_id,
            chapterName: chapter_name,
            errors: errors,
            timeSec: time
        );
    }
    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        if (!offlineMode)
        {
            AdaptiveSystemClient.OnDecisionReceived -= HandleDecision;
            AdaptiveSystemClient.OnSessionStarted -= SaveActiveChapters;
        }
        chapterTracker.ObservationDataReady -= SendObservation;

        //if (endIterationButton != null)
        //{
        //    endIterationButton.onClick.RemoveListener(OnEndIterationButtonPressed);
        //}
    }
}