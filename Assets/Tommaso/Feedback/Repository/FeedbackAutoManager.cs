using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Core.SceneObjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using System;
using System.ComponentModel.Design;
using UnityEngine.UI;
using static FeedbackRepository;
using UnityEngine.Video;
using VRBuilder.Core.Behaviors;

public class FeedbackAutoManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public FeedbackSetHolder feedbackHolder;

    public FeedbackDisplayer feedbackDisplayer;

    // Dizionario: Feedback attivo → Step ancora da completare
    private Dictionary<FeedbackRepository.FeedbackData, HashSet<string>> activeFeedbackSteps = new();
    private HashSet<FeedbackRepository.FeedbackData> shownFeedbacks = new();

    private IProcess process;
    private void OnEnable()
    {
        if (ProcessRunner.Current != null)
        {
            process = ProcessRunner.Current;
            RegisterToStepEvents();
        }
        else
        {
            ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        }
    }

    private void OnDisable()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = args.Process;
        RegisterToStepEvents();
    }

    private void RegisterToStepEvents()
    {
        if (process == null || process.Data == null)
        {
            Debug.LogError("[FeedbackAutoManager] Nessun processo valido per la registrazione degli step.");
            return;
        }

        if (feedbackHolder?.FeedbackRepository == null)
        {
            Debug.LogError("[FeedbackAutoManager] FeedbackRepository non assegnato.");
            return;
        }

        LearningProfile profile = GetComponent<LearningProfile>();
        if (profile == null)
        {
            Debug.LogWarning("[FeedbackAutoManager] LearningProfile non trovato sul GameObject.");
            return;
        }

        // 1️⃣ Ottieni il profilo dell’utente
        var profileTuple = profile.GetProfileTuple();

        // 2️⃣ Ottieni solo i feedback appartenenti al percorso corretto
        var feedbackList = feedbackHolder.FeedbackRepository.GetAllFeedbacksForProfile(profileTuple);

        // 3️⃣ Costruisci la mappa step → feedback
        var feedbackMap = new Dictionary<string, FeedbackRepository.FeedbackData>();
        foreach (var fb in feedbackList)
        {
            foreach (var stepName in fb.StepForCompletition)
            {
                if (!feedbackMap.ContainsKey(stepName))
                    feedbackMap[stepName] = fb;
            }
        }

        int mainChapterCount = 0;
        int subChapterCount = 0;
        int totalStepCount = 0;

        // 4️⃣ Scorri i capitoli principali
        foreach (var chapter in process.Data.Chapters)
        {
            if (chapter == null)
                continue;

            mainChapterCount++;
            string chapterName = chapter.Data.Name;

            // --- Step del capitolo principale ---
            foreach (var stepChild in chapter.Data.Steps)
            {
                if (stepChild is IStep step)
                {
                    string stepName = step.Data.Name;
                    if (!feedbackMap.ContainsKey(stepName))
                        continue; // salta se non ha feedback

                    var feedback = feedbackMap[stepName];

                    step.LifeCycle.StageChanged += (sender, args) =>
                    {
                        if (args.Stage == Stage.Activating)
                            OnStepActivated(step, chapterName, feedback);
                        else if (args.Stage == Stage.Inactive)
                            HandleStepCompletion(stepName);
                    };

                    totalStepCount++;
                }
            }

            // --- Subchapter (ricorsione) ---
            RegisterSubChaptersWithSeparateCount(chapter, feedbackMap, ref subChapterCount, ref totalStepCount);
        }

        Debug.Log($"[FeedbackAutoManager] Registrati {mainChapterCount} capitoli principali, {subChapterCount} sottocapitoli e {totalStepCount} step con feedback associato per il profilo {profileTuple.visivoVerbale}, {profileTuple.attivoRiflessivo}, {profileTuple.sequenzialeGlobale}.");
    }



    private void RegisterSubChaptersWithSeparateCount(
        IChapter chapter,
        Dictionary<string, FeedbackRepository.FeedbackData> feedbackMap,
        ref int subChapterCount,
        ref int totalStepCount)
    {
        if (chapter?.Data?.Steps == null)
            return;

        foreach (var stepChild in chapter.Data.Steps)
        {
            if (stepChild is IStep step)
            {
                foreach (var behavior in step.Data.Behaviors.Data.Behaviors)
                {
                    if (behavior is ExecuteChaptersBehavior exec)
                    {
                        foreach (var sub in exec.Data.SubChapters)
                        {
                            if (sub?.Chapter == null)
                                continue;

                            var subChapter = sub.Chapter;
                            subChapterCount++;

                            foreach (var subStepChild in subChapter.Data.Steps)
                            {
                                if (subStepChild is IStep subStep)
                                {
                                    string subStepName = subStep.Data.Name;
                                    string subChapterName = subChapter.Data.Name;

                                    // 🔹 Filtra solo gli step con feedback associato
                                    if (!feedbackMap.ContainsKey(subStepName))
                                        continue;

                                    var feedback = feedbackMap[subStepName];

                                    subStep.LifeCycle.StageChanged += (sender, args) =>
                                    {
                                        if (args.Stage == Stage.Activating)
                                            OnStepActivated(subStep, subChapterName, feedback);
                                        else if (args.Stage == Stage.Inactive)
                                            HandleStepCompletion(subStepName);
                                    };

                                    totalStepCount++;
                                }
                            }

                            // 🔁 Ricorsione per sottocapitoli annidati
                            RegisterSubChaptersWithSeparateCount(subChapter, feedbackMap, ref subChapterCount, ref totalStepCount);
                        }
                    }
                }
            }
        }
    }






    private void OnStepActivated(IStep step, string chapterName, FeedbackRepository.FeedbackData feedback)
    {
        string stepName = step.Data.Name;

        if (shownFeedbacks.Contains(feedback))
            return;

        // Usa direttamente lo step passato
        GameObject target = GetFirstGameObjectFromStep(step);

        if (target == null)
        {
            Debug.LogWarning($"[FeedbackAutoManager] Nessun GameObject target trovato per '{stepName}' nel capitolo '{chapterName}'.");
            return;
        }

        Transform feedbackPosition = feedbackDisplayer.FindFeedbackPositionChild(target);
        feedbackDisplayer.PrepareAndDisplayFeedback(feedback, feedbackPosition, feedbackHolder);

        if (!activeFeedbackSteps.ContainsKey(feedback))
            activeFeedbackSteps[feedback] = new HashSet<string>(feedback.StepForCompletition);

        shownFeedbacks.Add(feedback);
    }


    private void HandleStepCompletion(string stepName)
    {
        var feedbacksToRemove = new List<FeedbackRepository.FeedbackData>();

        foreach (var kvp in activeFeedbackSteps)
        {
            var feedback = kvp.Key;
            var remainingSteps = kvp.Value;

            if (remainingSteps.Contains(stepName))
            {
                remainingSteps.Remove(stepName);
                Debug.Log($"[FeedbackAutoManager] Step '{stepName}' rimosso dai step rimanenti del feedback '{feedback.FeedbackName}'.");
            }

            // Se tutti gli step di questo feedback sono completati
            if (remainingSteps.Count == 0)
            {
                Debug.Log($"[FeedbackAutoManager] Tutti gli step completati per '{feedback.FeedbackName}', chiudo il feedback.");

                // Trova il prefab del feedback attivo in scena
                FeedbackPrefabController prefab = FindFeedbackInstance(feedback.FeedbackName);

                if (prefab != null)
                {
                    prefab.CloseFeedback();
                    Debug.Log($"[FeedbackAutoManager] Feedback prefab '{feedback.FeedbackName}' chiuso con animazione.");
                }
                else
                {
                    Debug.LogWarning($"[FeedbackAutoManager] Nessuna istanza trovata per '{feedback.FeedbackName}'.");
                }

                feedbacksToRemove.Add(feedback);
            }
        }

        // Rimuovi i feedback completati dal dizionario
        foreach (var f in feedbacksToRemove)
        {
            activeFeedbackSteps.Remove(f);
            shownFeedbacks.Remove(f);
        }
    }

        private FeedbackPrefabController FindFeedbackInstance(string feedbackName)
    {
            FeedbackPrefabController[] allFeedbacks = FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);

        foreach (var fb in allFeedbacks)
        {
            // Confronta i nomi, oppure puoi aggiungere un campo "feedbackName" al prefab per più robustezza
            if (fb.name.Contains(feedbackName))
            {
                return fb;
            }
        }

        return null;
    }



    
}
