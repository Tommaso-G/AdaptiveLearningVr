// GameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRBuilder.Core;
using System.Linq;
using VRBuilder.Core.Behaviors;

public class GameManager : MonoBehaviour
{
    [Header("Chapter Configuration")]
    [SerializeField] private List<ChapterConfigData> allChapters;
    // Assegna nell'Inspector la lista completa di tutti i capitoli,
    // obbligatori e opzionali, nell'ordine in cui vuoi aggiungerli.
    [SerializeField] private List<string> chaptersToExclude = new List<string>();

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "ScenaUfficiale";
    [SerializeField] private ChaptersOrderManager chaptersOrderMgr = null;
    [SerializeField] private ChapterTracker chapterTracker = null;
    [SerializeField] private FeedbackChapterFilter feedbackChapterFilter = null;

    private IProcess process;
    public Dictionary<string, string> chaptersIdToName = new Dictionary<string, string>();

    public bool restartAll = false;

    private void Start()
    {
        AdaptiveSystemClient.OnDecisionReceived += HandleDecision;
        if (restartAll)
        {
            //restartAll = false;
            SessionPersistence.Clear();
        }
        chapterTracker.setChaptersToExclude(chaptersToExclude);
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
        if (SessionPersistence.HasSavedSession())
        {
            print("RE-inizializzazione sessione");            // Run successiva alla prima: recupera lo stato dal server
            string sessionId = SessionPersistence.Load();
            yield return StartCoroutine(
                RestoreAndInitialize(sessionId)
            );
        }
        else
        {
            print("Prima inizializzazione sessione");
            // Prima run: avvia nuova sessione
            yield return StartCoroutine(
                StartNewSession()
            );
        }
    }

    private IEnumerator StartNewSession()
    {
        bool done = false;

        if (allChapters.Count == 0)
        {
            allChapters = new List<ChapterConfigData>();

            for (int i = 0; i < process.Data.Chapters.Count; i++)
            {
                var c = process.Data.Chapters[i];

                if (chaptersToExclude.Contains(c.Data.Name))
                {
                    continue;
                }

                allChapters.Add(new ChapterConfigData
                {
                    chapter_id = c.ChapterMetadata.Guid.ToString(), // ID del capitolo VRBuilder
                    name = c.Data.Name,                // Nome del capitolo
                    is_mandatory = !c.Data.Name.Trim()
                         .ToLower()
                         .Contains("optional"), // Se è obbligatorio
                    weight = 1.0f // Puoi aggiungere weight e max_iterations se vuoi
                });
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

            AdaptiveSystemClient.Instance.StartSession(
                allChapters.ToArray(),
                activeChapters =>
                {
                    // Salva solo il session_id
                    SessionPersistence.Save(
                        AdaptiveSystemClient.Instance.GetSessionId()
                    );

                    //FlowManager.Instance.InitializeWithChapters(activeChapters);
                    // feedback massimo per tutti alla prima run
                    foreach (var chapter in feedbackChapterFilter.chapterSettings)
                    {
                        chapter.feedbackLevel = 0;
                    }
                    done = true;
                }
            );
        yield return new WaitUntil(() => done);
    }

    private IEnumerator RestoreAndInitialize(string sessionId)
    {
        bool done = false;
        AdaptiveSystemClient.Instance.RestoreSession(
            sessionId,
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

                // Inizializza VRBuilder con i capitoli attivi
                //FlowManager.Instance.InitializeWithChapters(
                //    state.active_chapters
                //);

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

                // Imposta il feedback per ogni capitolo
                foreach (var chapterData in state.chapter_details)
                {
                    if (chaptersIdToName.TryGetValue(chapterData.chapter_id, out string name))
                    {
                        feedbackChapterFilter.setFeedbackLevel(name, chapterData.feedback_level);
                    }
                }

                string message = "Capitoli attivi per l'iterazione: \n";
                List<string> chapterToAdd = new List<string>();
                foreach (string ac in state.active_chapters)
                {
                    message += "- Capitolo: " + chaptersIdToName[ac] + "\n";
                    if (chaptersIdToName[ac].Contains("Optional"))
                    {
                        chapterToAdd.Add(ac);
                    }
                }

                AddOptionalChapter(chapterToAdd);

                print(message);
                done = true;
            }
        );
        yield return new WaitUntil(() => done);
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

        if (decision.add_optional)
        {
            Debug.Log("[ChapterTracker] Nuovo capitolo opzionale attivato");
        }


        // Se è stato rimosso un capitolo opzionale
        if (decision.remove_optional)
        {
            chaptersOrderMgr.RemoveChapter(chapterToRemoveName: chaptersIdToName[decision.removed_chapter_id]);
            Debug.Log("[ChapterTracker] Capitolo opzionale rimosso: " + chaptersIdToName[decision.removed_chapter_id]);
        }

        //// Se il capitolo è padroneggiato, puoi mostrare un feedback positivo
        //if (decision.chapter_mastered)
        //{
        //    Debug.Log($"[ChapterTracker] capitolo padroneggiato!");
        //    //UIManager.Instance.ShowMasteryBadge(chapterId);
        //}
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ReloadSceneForNextIteration();
        }
    }

    private void AddOptionalChapter(List<string> chapterToAdd)
    {
        foreach(string id in chapterToAdd)
        {
            chaptersOrderMgr.AddOptional(chaptersIdToName[id]);
        }
    }
    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        AdaptiveSystemClient.OnDecisionReceived -= HandleDecision;
    }
}