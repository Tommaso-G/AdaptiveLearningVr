using System.Collections.Generic;
using UnityEngine;

public class FeedbackIconsManager : MonoBehaviour
{
    public LearningProfile learningProfile;

    [System.Serializable]
    public class StepIconMapping
    {
        [HideInInspector]
        public string stepName;
        public List<FeedbackIconController> iconControllers = new List<FeedbackIconController>();
    }

    [System.Serializable]
    public class ChapterStepIconMapping
    {
        [HideInInspector]
        public string chapterName;
        public List<StepIconMapping> steps = new List<StepIconMapping>();
    }

    [Header("Step → FeedbackIconController")]
    public List<ChapterStepIconMapping> chapterStepMappings = new List<ChapterStepIconMapping>();

    [Header("Debug / Override")]
    [Tooltip("Se true, in modalità Sequenziale i waypoint sono sempre tutti disattivati.")]
    public bool forceDisableSequentialIcons = true;

    void Start()
    {
        ApplyLearningStyle();
        DeactivateAllWaypoints();
    }

    // ─────────────────────────────────────────────────────────────────
    // LEARNING STYLE
    // ─────────────────────────────────────────────────────────────────

    public void ApplyLearningStyle()
    {
        if (learningProfile == null)
        {
            Debug.LogError("[FeedbackIconsManager] LearningProfile non assegnato.");
            return;
        }

        var profile = learningProfile.GetProfileTuple();
        bool isVisivo = profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;

        FeedbackIconController[] controllers = FindObjectsByType<FeedbackIconController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var controller in controllers)
            controller.SetMode(isVisivo);
    }

    // ─────────────────────────────────────────────────────────────────
    // WAYPOINT — unico punto di ingresso, chiamato per ogni step
    // (sia chapter normali che subchapter)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chiamare ogni volta che uno step si attiva, indipendentemente
    /// dal fatto che sia in un chapter principale o in un subchapter.
    /// </summary>
    public void OnStepStarted(string chapterName, string stepName)
    {
        if (IsSequenziale() && forceDisableSequentialIcons)
        {
            DeactivateAllWaypoints();
            return;
        }

        // Disattiva tutti i waypoint e attiva solo quello dello step corrente
        DeactivateAllWaypoints();
        ActivateWaypointsForStep(chapterName, stepName);
    }

    /// <summary>
    /// Chiamare quando si entra in un nuovo chapter (non subchapter).
    /// Disattiva tutti i waypoint — verranno riattivati da OnStepStarted.
    /// </summary>
    public void OnChapterStarted(string chapterName)
    {
        DeactivateAllWaypoints();
    }

    // ─────────────────────────────────────────────────────────────────
    // TODO — da implementare quando noto
    // ─────────────────────────────────────────────────────────────────

    public void OnStepCompleted(string chapterName, string stepName)
    {
        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));
        if (chapter == null) return;

        StepIconMapping mapping = chapter.steps.Find(m =>
            string.Equals(m.stepName, stepName, System.StringComparison.OrdinalIgnoreCase));
        if (mapping == null || mapping.iconControllers.Count == 0) return;

    }

    // ─────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────

    public void ActivateWaypointsForStep(string chapterName, string stepName)
    {
        if (IsSequenziale() && forceDisableSequentialIcons)
            {
                Debug.Log("[FeedbackIconsManager] ActivateWaypointsForStep bloccato — forceDisableSequentialIcons attivo.");
                return;
            }
        
        Debug.Log($"[FIM] ActivateWaypointsForStep chiamato | sequenziale: {IsSequenziale()} | forceDisable: {forceDisableSequentialIcons}");

        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (chapter == null)
        {
            Debug.Log($"[FeedbackIconsManager] Nessun mapping per capitolo '{chapterName}'.");
            return;
        }

        StepIconMapping mapping = chapter.steps.Find(m =>
            string.Equals(m.stepName, stepName, System.StringComparison.OrdinalIgnoreCase));

        if (mapping == null)
        {
            Debug.Log($"[FeedbackIconsManager] Nessun mapping per step '{stepName}' in '{chapterName}'.");
            return;
        }

        foreach (var controller in mapping.iconControllers)
        {
            if (controller == null) continue;
            GameObject wp = GetWaypointFromController(controller);
            if (wp != null)
            {
                wp.SetActive(true);
                Debug.Log($"[FeedbackIconsManager] Waypoint attivato: '{wp.name}' per step '{stepName}'.");
            }
        }
    }


    private void DeactivateAllWaypoints()
    {
        FeedbackIconController[] allControllers = FindObjectsByType<FeedbackIconController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var controller in allControllers)
            DeactivateWaypoint(controller);
    }

    public void DeactivateWaypointsForChapter(string chapterName)
    {
        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (chapter == null) return;

        foreach (var mapping in chapter.steps)
            foreach (var controller in mapping.iconControllers)
            {
                if (controller == null) continue;
                DeactivateWaypoint(controller);
            }
    }
    private GameObject GetWaypointFromController(FeedbackIconController controller)
    {
        Transform iconFeedback = FindChildWithTag(controller.transform, "IconFeedback");
        if (iconFeedback == null) return null;

        Transform waypoint = FindChildWithTag(iconFeedback, "WayPointSmall");
        if (waypoint == null) return null;

        return waypoint.gameObject;
    }

    private void DeactivateWaypoint(FeedbackIconController controller)
    {
        if (fader != null)
            fader.Fade();

        GameObject wp = GetWaypointFromController(controller);
        if (wp != null)
            wp.SetActive(false);
    }

    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
                return child;
        }
        return null;
    }

    private bool IsSequenziale()
    {
        if (learningProfile == null) return true;
        return learningProfile.GetProfileTuple().sequenzialeGlobale
               == LearningEnums.SequenzialeGlobale.Sequenziale;
    }
}