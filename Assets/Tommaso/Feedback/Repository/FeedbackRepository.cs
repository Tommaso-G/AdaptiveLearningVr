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
using Unity.VisualScripting.FullSerializer;

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

    [Header("Feedback Eccezionali")]
    public List<ExceptionFeedbackEntry> exceptionFeedbacks = new List<ExceptionFeedbackEntry>();



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

        [Header("Pagine del feedback")]
        public List<FeedbackPage> pages = new List<FeedbackPage>();

        [Header("Step di completamento associati (0 o più)")]
        [StepForCompletionDropdown]
        public List<string> StepForCompletition = new List<string>();

        [Header("Prefab Personalizzato, se inserito tutto il resto viene ignorato")]
        public GameObject PersonalizedPrefab;

        [Header("Se è true, il feedback ha un bottone che va premnuto per proseguire")]
        public bool needsButtonToBeCompleted = false;

        public bool applyReflectiveEffects = true;
    }

    [Serializable]
    public class FeedbackPage
    {
        [Tooltip("Tipologia del feedback (modello FSLSM")]
        public LearningEnums.SequenzialeGlobale Sequenzale_Globale;
        public LearningEnums.VisivoVerbale Visivo_Verbale;

        public bool isIntroductory = false;

        [Tooltip("Una singola immagine opzionale per la pagina.")]
        public Sprite image;

        [Tooltip("Un singolo video opzionale per la pagina.")]
        public VideoClip video;

        [Tooltip("Testo opzionale della pagina.")]
        [TextArea(2, 5)]
        public string text;
    }

    [Serializable]
    public class ExceptionFeedbackEntry
    {
        [Header("Percorsi per profilo (stessa struttura di globalPath / sequentialPath)")]
        public PathGroup globalPath = new PathGroup();
        public PathGroup sequentialPath = new PathGroup();
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

        // Selezione del percorso principale (Globale o Sequenziale)
        PathGroup branch = (sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale)
            ? globalPath
            : sequentialPath;

        List<Chapter> chapters = null;

        // Selezione ramo Visivo/Verbale e Attivo/Riflessivo
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

        // Ricerca del capitolo per nome (case-insensitive)
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

        //Cerca il feedback associato allo step specificato
        FeedbackData feedback = chapter.feedbacks
            .FirstOrDefault(f => f.StepForCompletition.Contains(stepName));

        if (feedback != null)
        {
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

        // Seleziona subito il percorso (Globale o Sequenziale)
        PathGroup pathGroup = profileTuple.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale
            ? globalPath
            : sequentialPath;

        // Seleziona la modalità Visuale o Verbale
        bool isVisual = profileTuple.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;
        bool isActive = profileTuple.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;

        // Naviga direttamente nel ramo corretto
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

    /// <summary>
    /// Cerca in exceptionFeedbacks il FeedbackData con il nome specificato,
    /// selezionando il percorso corretto in base al profilo.
    /// </summary>
    public FeedbackData GetExceptionFeedback(
        string feedbackName,
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
         LearningEnums.SensitivoIntuitivo _,
         LearningEnums.VisivoVerbale visivoVerbale,
         LearningEnums.SequenzialeGlobale sequenzialeGlobale) profileTuple)
    {
        if (string.IsNullOrEmpty(feedbackName))
        {
            Debug.LogWarning("[FeedbackRepository] feedbackName nullo o vuoto.");
            return null;
        }

        var chapters = GetExceptionChapters(profileTuple);
        if (chapters == null) return null;

        foreach (var chapter in chapters)
        {
            var feedback = chapter.feedbacks?.FirstOrDefault(f =>
                string.Equals(f.FeedbackName, feedbackName, StringComparison.OrdinalIgnoreCase));

            if (feedback != null) return feedback;
        }

        Debug.LogWarning($"[FeedbackRepository] Nessun feedback eccezionale con nome '{feedbackName}' trovato nel percorso selezionato.");
        return null;
    }

    /// <summary>
    /// Restituisce il chapterName del capitolo che contiene il feedback con il nome specificato,
    /// selezionando il percorso corretto in base al profilo.
    /// Usato da ExceptionFeedbackHandler per il controllo su FeedbackChapterFilter.
    /// </summary>
    public string GetExceptionChapterName(
        string feedbackName,
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
         LearningEnums.SensitivoIntuitivo _,
         LearningEnums.VisivoVerbale visivoVerbale,
         LearningEnums.SequenzialeGlobale sequenzialeGlobale) profileTuple)
    {
        var chapters = GetExceptionChapters(profileTuple);
        if (chapters == null) return null;

        foreach (var chapter in chapters)
        {
            bool found = chapter.feedbacks?.Any(f =>
                string.Equals(f.FeedbackName, feedbackName, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (found) return chapter.ChapterName;
        }

        return null;
    }

    /// <summary>
    /// Seleziona la lista di capitoli corretta dall'unico ExceptionFeedbackEntry
    /// in base al profilo FSLSM.
    /// </summary>
    private List<Chapter> GetExceptionChapters(
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
         LearningEnums.SensitivoIntuitivo _,
         LearningEnums.VisivoVerbale visivoVerbale,
         LearningEnums.SequenzialeGlobale sequenzialeGlobale) profileTuple)
    {
        if (exceptionFeedbacks == null || exceptionFeedbacks.Count == 0)
        {
            Debug.LogWarning("[FeedbackRepository] exceptionFeedbacks è vuoto.");
            return null;
        }

        var entry = exceptionFeedbacks[0];
        var (attivoRiflessivo, _, visivoVerbale, sequenzialeGlobale) = profileTuple;

        PathGroup branch = (sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale)
            ? entry.globalPath
            : entry.sequentialPath;

        if (visivoVerbale == LearningEnums.VisivoVerbale.Visivo)
            return (attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo)
                ? branch.visualPath.attivo
                : branch.visualPath.riflessivo;
        else
            return (attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo)
                ? branch.verbalPath.attivo
                : branch.verbalPath.riflessivo;
    }



    // ======================= STATIC HELPERS =========================

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
                // SingleSceneObjectReference
                if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
                {
                    var reference = prop.GetValue(data) as SingleSceneObjectReference;
                    if (reference == null) continue;

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
                    if (mspr == null) continue;

                    var guidsProp = prop.PropertyType.GetProperty("Guids");
                    if (guidsProp == null) continue;

                    var guids = guidsProp.GetValue(mspr) as IEnumerable<Guid>;
                    if (guids == null) continue;

                    foreach (var guid in guids)
                    {
                        var sceneObject = registry.GetObjects(guid).FirstOrDefault();
                        if (sceneObject != null && sceneObject.GameObject != null)
                        {
                            return sceneObject.GameObject;
                        }
                    }
                }
            }

            return null;
        }

        // Controlla Behavior
        foreach (var behavior in step.Data.Behaviors.Data.Behaviors)
        {
            var go = CheckProperties(behavior.Data, $"behavior '{behavior.Data.GetType().Name}'");
            if (go != null) return go;
        }

        // Controlla Conditions
        foreach (var transition in step.Data.Transitions.Data.Transitions)
        {
            foreach (var condition in transition.Data.Conditions)
            {
                var go = CheckProperties(condition.Data, $"condition '{condition.Data.GetType().Name}'");
                if (go != null) return go;
            }
        }

        Debug.Log($"[StepHelpers] <<< Nessun GameObject trovato nello step '{step.Data.Name}'");
        return null;
    }



    // ======================= ONVALIDATE =========================

    private void OnValidate()
    {
        if (globalPath != null && sequentialPath != null)
        {
            UpdateAllFeedbackPages(globalPath, LearningEnums.SequenzialeGlobale.Globale);
            UpdateAllFeedbackPages(sequentialPath, LearningEnums.SequenzialeGlobale.Sequenziale);
        }

        // Aggiorna anche i percorsi interni agli exception feedbacks
        if (exceptionFeedbacks != null)
        {
            foreach (var entry in exceptionFeedbacks)
            {
                if (entry == null) continue;
                UpdateAllFeedbackPages(entry.globalPath, LearningEnums.SequenzialeGlobale.Globale);
                UpdateAllFeedbackPages(entry.sequentialPath, LearningEnums.SequenzialeGlobale.Sequenziale);
            }
        }
    }

    private void UpdateAllFeedbackPages(PathGroup pathGroup, LearningEnums.SequenzialeGlobale seqGlob)
    {
        if (pathGroup == null) return;

        // VISUAL
        UpdateChapters(pathGroup.visualPath.attivo, seqGlob, LearningEnums.VisivoVerbale.Visivo);
        UpdateChapters(pathGroup.visualPath.riflessivo, seqGlob, LearningEnums.VisivoVerbale.Visivo);

        // VERBAL
        UpdateChapters(pathGroup.verbalPath.attivo, seqGlob, LearningEnums.VisivoVerbale.Verbale);
        UpdateChapters(pathGroup.verbalPath.riflessivo, seqGlob, LearningEnums.VisivoVerbale.Verbale);
    }

    private void UpdateChapters(List<Chapter> chapters,
        LearningEnums.SequenzialeGlobale seqGlob,
        LearningEnums.VisivoVerbale visVerb)
    {
        if (chapters == null) return;

        foreach (var chapter in chapters)
        {
            if (chapter.feedbacks == null) continue;

            foreach (var feedback in chapter.feedbacks)
            {
                if (feedback.pages == null) continue;

                foreach (var page in feedback.pages)
                {
                    if (page == null) continue;

                    page.Sequenzale_Globale = seqGlob;
                    page.Visivo_Verbale = visVerb;
                }
            }
        }
    }
}