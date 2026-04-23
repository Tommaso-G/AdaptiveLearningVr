// GameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRBuilder.Core;
using System.Linq;
using VRBuilder.Core.Behaviors;
using UnityEngine.UI;
using static UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile;

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
    [SerializeField] private FeedbackChapterFilter feedbackChapterFilter = null;
    [SerializeField] private DifficultyChapterFilter difficultyChapterFilter = null;

    //[Header("Iteration Control")]
    //[SerializeField] private Button endIterationButton = null;

    [Header("Session Control")]
    [SerializeField] public bool resetAll = false;

    private IProcess process;
    private Dictionary<string, string> chaptersIdToName = new Dictionary<string, string>();

    private int currentIterationNumber = 1;
    private List<string> chaptersCompletedThisIteration = new List<string>();
    private List<string> active_chapters = new List<string>();

    private void Start()
    {
        chaptersOrderMgr = FindFirstObjectByType<ChaptersOrderManager>();
        chapterTracker = FindFirstObjectByType<ChapterTracker>();
        feedbackChapterFilter = FindFirstObjectByType<FeedbackChapterFilter>();
        difficultyChapterFilter = FindFirstObjectByType<DifficultyChapterFilter>();

        AdaptiveSystemClient.OnDecisionReceived += HandleDecision;
        AdaptiveSystemClient.OnSessionStarted += SaveActiveChapters;

        if (resetAll)
        {
            Debug.Log("[GameManager] resetAll=true: Eliminando sessione precedente");
            SessionPersistence.Clear();
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
        //bool resetAll = SessionPersistence.GetResetAll();

        //if (resetAll || !SessionPersistence.HasSavedSession())
        //{
        //    Debug.Log("[GameManager] Nuova sessione");
        //    SessionPersistence.SetResetAll(false); // IMPORTANTISSIMO
        //    yield return StartCoroutine(StartNewSession());
        //}
        //else
        //{
        //    Debug.Log("[GameManager] Riprendo sessione esistente");
        //    string sessionId = SessionPersistence.Load();
        //    yield return StartCoroutine(RestoreAndInitialize(sessionId));
        //}

        if (resetAll || !SessionPersistence.HasSavedSession())
        {
            Debug.Log("[GameManager] resetAll=true: Avviando nuova sessione");
            yield return StartCoroutine(StartNewSession());
        }
        else
        {
            Debug.Log("[GameManager] resetAll=false: Riprendendo sessione esistente");
            string sessionId = SessionPersistence.Load();
            yield return StartCoroutine(RestoreAndInitialize(sessionId));
        }
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

                        // versione difficoltà minima per la prima run
                        //foreach (var chapter in difficultyChapterFilter.chapterSettings)
                        //{
                        //    chapter.difficultyLevel = 0;
                        //}

                        List<string> chapterToAdd = new List<string>();
                        foreach (string ac in activeChapters)
                        {
                            if (chaptersIdToName[ac].Contains("Optional"))
                            {
                                chapterToAdd.Add(ac);
                            }
                        }

                        AddOptionalChapter(chapterToAdd);

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
                    // se il server è sempre attivo, ma gestiamo il caso
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



                // Imposta il feedback e la difficoltà per ogni capitolo
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
            }
        );
        yield return new WaitUntil(() => done);
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

        StartCoroutine(EndIterationCoroutine(active_chapters));
    }

    // ===== NUOVO: Chiama il server per marcare fine iterazione =====
    private IEnumerator EndIterationCoroutine(List<string> activeChapters)
    {
        yield return StartCoroutine(
            AdaptiveSystemClient.Instance.EndIteration(
                activeChapters.ToArray(),
                result =>
                {
                    if (result.status == "iteration_complete")
                    {
                        Debug.Log(
                            $"[GameManager] ✓ Iterazione {result.iteration_number} COMPLETA! " +
                            $"Prossima: {result.next_iteration}"
                        );
                        currentIterationNumber = result.next_iteration;
                    }
                    else if (result.status == "iteration_incomplete")
                    {
                        Debug.LogWarning(
                            $"[GameManager] ⚠️ Iterazione INCOMPLETA! " +
                            $"Mancanti: {string.Join(", ", result.incomplete_chapters)}"
                        );
                    }
                }
            )
        );
        //ReloadSceneForNextIteration();
    }

    /// <summary>
    /// Ricarica la scena al termine di un'iterazione.
    /// Chiamato da FlowManager quando tutti i capitoli attivi sono completati.
    /// </summary>
    public void ReloadSceneForNextIteration()
    {
        // Lo stato è già salvato sul server Python.
        // Basta ricaricare la scena: GameManager.Start()
        // troverà la sessione salvata e recupererà lo stato aggiornato.
        Debug.Log("[GameManager] Ricaricare la scena per l'iterazione successiva...");
        //SessionPersistence.SetResetAll(false);
        SceneManager.LoadScene(gameSceneName);
    }

    public void HandleDecision(DecisionResponse decision)
    {
        // Aggiorna il feedback visivo in base al livello deciso dalla BN
        //FeedbackManager.Instance.SetFeedbackLevel(chapterId, decision.feedback_level);
        if (decision.message != null)
        {
            print(decision.message);
        }

        // se è cambiato il livello di feedback aggiorna
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


        // Se è stato rimosso un capitolo opzionale
        if (decision.remove_optional)
        {
            chaptersOrderMgr.RemoveChapter(chapterToRemoveName: chaptersIdToName[decision.removed_chapter_id]);
            active_chapters.Remove(chaptersIdToName[decision.removed_chapter_id]);
            Debug.Log("[GameManager] Capitolo opzionale rimosso: " + chaptersIdToName[decision.removed_chapter_id]);
        }

        //// Se il capitolo è padroneggiato, puoi mostrare un feedback positivo
        //if (decision.chapter_mastered)
        //{
        //    Debug.Log($"[ChapterTracker] capitolo padroneggiato!");
        //    //UIManager.Instance.ShowMasteryBadge(chapterId);
        //}
    }

    private void SaveActiveChapters(string[] response)
    {

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
        // ===== MODIFICATO: Spazio per ricaricare la scena =====
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log("[GameManager] Spazio premuto: ricaricare scena");
            ReloadSceneForNextIteration();
        }

        if (Input.GetKeyUp(KeyCode.Z))
        {
            Debug.Log("[GameManager] Chiamata End Iteraction");
            OnEndIterationButtonPressed();
        }

        if (Input.GetKeyUp(KeyCode.N))
        {
            Debug.Log("[GameManager] Nuova sessione richiesta");

            SessionPersistence.Clear();
            SessionPersistence.SetResetAll(true);

            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void AddOptionalChapter(List<string> chapterToAdd)
    {
        foreach (string id in chapterToAdd)
        {
            print("[GameManager] Capitolo da aggiungere: " + chaptersIdToName[id]);
            chaptersOrderMgr.AddOptional(chaptersIdToName[id]);
        }
    }
    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        AdaptiveSystemClient.OnDecisionReceived -= HandleDecision;

        //if (endIterationButton != null)
        //{
        //    endIterationButton.onClick.RemoveListener(OnEndIterationButtonPressed);
        //}
    }
}