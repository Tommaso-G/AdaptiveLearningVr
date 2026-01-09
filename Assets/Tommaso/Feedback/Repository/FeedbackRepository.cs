using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using VRBuilder.Core;
using UnityEngine.Video;
using System.Linq;
using VRBuilder.Core.Configuration;
using System.Reflection;
using VRBuilder.Core.SceneObjects;

[CreateAssetMenu(fileName = "FeedbackRepository", menuName = "VR Feedback/Unified Repository")]
public class FeedbackRepository : ScriptableObject
{
    [Header("Prefab (UI)")]
    [Tooltip("Prefab per feedback con una o più immagini statiche.")]
    public GameObject SingleContainer;

    [Tooltip("Prefab per feedback con un singolo video.")]
    public GameObject MultipleContainer;

    [Header("Percorsi principali")]
    public PathGroup globalPath = new PathGroup();
    public PathGroup sequentialPath = new PathGroup();

    // ======================= CLASSI ANNIDATE =========================

    [Serializable]
    public class PathGroup
    {
        [Header("Verbal")]
        public VerbalPath verbalPath = new VerbalPath();

        [Header("Visual")]
        public VisualPath visualPath = new VisualPath();
    }

    [Serializable]
    public class VerbalPath
    {
        [Header("Active")]
        public List<Chapter> attivo = new List<Chapter>();

        [Header("Reflective")]
        public List<Chapter> riflessivo = new List<Chapter>();
    }

    [Serializable]
    public class VisualPath
    {
        [Header("Active")]
        public List<Chapter> attivo = new List<Chapter>();

        [Header("Reflective")]
        public List<Chapter> riflessivo = new List<Chapter>();
    }

    [Serializable]
    public class Chapter
    {
        [ChapterDropdown]
        public string ChapterName = "";

        [Header("Feedback")]
        public List<FeedbackData> feedbacks = new List<FeedbackData>();
    }

    [Serializable]
    public class FeedbackData
    {
        [Header("Nome del feedback")]
        public string FeedbackName = "";

        [Header("Immagini (0 o più)")]
        public List<Sprite> images = new List<Sprite>();

        [Header("Video (0 o più)")]
        public List<VideoClip> videos = new List<VideoClip>();

        [Header("Step di completamento associati (0 o più)")]
        [StepForCompletionDropdown]
        public List<string> StepForCompletition = new List<string>();
    }

    // ======================= METODI GET =========================

    public FeedbackData GetFeedbackByStep(
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
        LearningEnums.SensitivoIntuitivo _,
        LearningEnums.VisivoVerbale visivoVerbale,
        LearningEnums.SequenzialeGlobale sequenzialeGlobale) profileTuple,
        string chapterName,
        string stepName)
    {
        var (attivoRiflessivo, _, visivoVerbale, sequenzialeGlobale) = profileTuple;

        // 1️⃣ Selezione del percorso principale (Globale o Sequenziale)
        PathGroup branch = (sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale)
            ? globalPath
            : sequentialPath;

        List<Chapter> chapters = null;

        // 2️⃣ Selezione ramo Visivo/Verbale e Attivo/Riflessivo
        if (visivoVerbale == LearningEnums.VisivoVerbale.Visivo)
        {
            chapters = (attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo)
                ? branch.visualPath.attivo
                : branch.visualPath.riflessivo;
        }
        else
        {
            chapters = (attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo)
                ? branch.verbalPath.attivo
                : branch.verbalPath.riflessivo;
        }

        if (chapters == null || chapters.Count == 0)
        {
            Debug.LogWarning("[FeedbackRepository] Nessun capitolo presente per questo percorso.");
            return null;
        }

        // 3️⃣ Ricerca del capitolo per nome (case-insensitive)
        Chapter chapter = chapters
            .FirstOrDefault(c => string.Equals(c.ChapterName, chapterName, StringComparison.OrdinalIgnoreCase));

        if (chapter == null)
        {
            Debug.LogWarning($"[FeedbackRepository] Nessun capitolo con nome '{chapterName}' trovato nel percorso selezionato.");
            return null;
        }

        if (chapter.feedbacks == null || chapter.feedbacks.Count == 0)
        {
            Debug.LogWarning($"[FeedbackRepository] Il capitolo '{chapter.ChapterName}' non contiene feedback.");
            return null;
        }

        // 4️⃣ Cerca il feedback associato allo step specificato
        FeedbackData feedback = chapter.feedbacks
            .FirstOrDefault(f => f.StepForCompletition.Contains(stepName));

        if (feedback != null)
        {
            Debug.Log($"[FeedbackRepository] Feedback '{feedback.FeedbackName}' trovato per lo step '{stepName}' nel capitolo '{chapter.ChapterName}'.");
            return feedback;
        }
        else
        {
            Debug.LogWarning($"[FeedbackRepository] Nessun feedback trovato per lo step '{stepName}' nel capitolo '{chapter.ChapterName}'.");
            return null;
        }
    }

    public IEnumerable<FeedbackData> GetAllFeedbacksForProfile((
        LearningEnums.AttivoRiflessivo attivoRiflessivo,
        LearningEnums.SensitivoIntuitivo sensitivoIntuitivo,
        LearningEnums.VisivoVerbale visivoVerbale,
        LearningEnums.SequenzialeGlobale sequenzialeGlobale
        ) profileTuple)
    {
        var allFeedbacks = new List<FeedbackData>();

        // 1️⃣ Seleziona subito il percorso (Globale o Sequenziale)
        PathGroup pathGroup = profileTuple.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale
            ? globalPath
            : sequentialPath;

        // 2️⃣ Seleziona la modalità Visuale o Verbale
        bool isVisual = profileTuple.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;
        bool isActive = profileTuple.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;

        // 3️⃣ Naviga direttamente nel ramo corretto
        if (isVisual)
        {
            if (isActive)
                allFeedbacks.AddRange(pathGroup.visualPath.attivo.SelectMany(ch => ch.feedbacks));
            else
                allFeedbacks.AddRange(pathGroup.visualPath.riflessivo.SelectMany(ch => ch.feedbacks));
        }
        else // Verbale
        {
            if (isActive)
                allFeedbacks.AddRange(pathGroup.verbalPath.attivo.SelectMany(ch => ch.feedbacks));
            else
                allFeedbacks.AddRange(pathGroup.verbalPath.riflessivo.SelectMany(ch => ch.feedbacks));
        }

        return allFeedbacks;
    }



    public static GameObject GetFirstGameObjectFromStep(IStep step)
    {
        if (step == null)
        {
            Debug.LogWarning("[StepHelpers] Step nullo passato a GetFirstGameObjectFromStep.");
            return null;
        }

        Debug.Log($"[StepHelpers] >>> Inizio ricerca GameObject nello step '{step.Data.Name}'");

        var registry = RuntimeConfigurator.Configuration.SceneObjectRegistry;

        // Funzione helper per analizzare le proprietà di un oggetto
        GameObject CheckProperties(object data, string ownerType)
        {
            var properties = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                Debug.Log($"[StepHelpers] Analizzando property: {prop.Name} ({prop.PropertyType.Name}) in {ownerType}");

                // SingleSceneObjectReference
                if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
                {
                    var reference = prop.GetValue(data) as SingleSceneObjectReference;
                    if (reference == null)
                    {
                        Debug.Log($"[StepHelpers] SingleSceneObjectReference nullo in property '{prop.Name}'");
                        continue;
                    }

                    var sceneObject = reference.Value;
                    if (sceneObject != null && sceneObject.GameObject != null)
                    {
                        Debug.Log($"[StepHelpers] Trovato GameObject '{sceneObject.GameObject.name}' in SingleSceneObjectReference '{prop.Name}'");
                        return sceneObject.GameObject;
                    }
                    else
                    {
                        Debug.Log($"[StepHelpers] SingleSceneObjectReference non risolto o GameObject nullo in property '{prop.Name}'");
                    }
                }
                // MultipleScenePropertyReference<T>
                else if (prop.PropertyType.IsGenericType &&
                         prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
                {
                    var mspr = prop.GetValue(data);
                    if (mspr == null)
                    {
                        Debug.Log($"[StepHelpers] MultipleScenePropertyReference nullo in property '{prop.Name}'");
                        continue;
                    }

                    // Recupera la proprietà "Guids"
                    var guidsProp = prop.PropertyType.GetProperty("Guids");
                    if (guidsProp == null)
                    {
                        Debug.Log($"[StepHelpers] Proprietà 'Guids' non trovata in '{prop.Name}'");
                        continue;
                    }

                    var guids = guidsProp.GetValue(mspr) as IEnumerable<Guid>;
                    if (guids == null)
                    {
                        Debug.Log($"[StepHelpers] Nessun GUID trovato in MultipleScenePropertyReference '{prop.Name}'");
                        continue;
                    }

                    foreach (var guid in guids)
                    {
                        var sceneObject = registry.GetObjects(guid).FirstOrDefault();
                        if (sceneObject != null && sceneObject.GameObject != null)
                        {
                            Debug.Log($"[StepHelpers] Trovato GameObject '{sceneObject.GameObject.name}' in MultipleScenePropertyReference '{prop.Name}'");
                            return sceneObject.GameObject;
                        }
                    }
                }
            }

            return null;
        }

        // 1️⃣ Controlla Behavior
        foreach (var behavior in step.Data.Behaviors.Data.Behaviors)
        {
            var go = CheckProperties(behavior.Data, $"behavior '{behavior.Data.GetType().Name}'");
            if (go != null) return go;
        }

        // 2️⃣ Controlla Conditions
        foreach (var transition in step.Data.Transitions.Data.Transitions)
        {
            Debug.Log($"[StepHelpers] Analizzando transition: {transition.Data.GetType().Name}");
            foreach (var condition in transition.Data.Conditions)
            {
                var go = CheckProperties(condition.Data, $"condition '{condition.Data.GetType().Name}'");
                if (go != null) return go;
            }
        }

        Debug.Log($"[StepHelpers] <<< Nessun GameObject trovato nello step '{step.Data.Name}'");
        return null;
    }



}




