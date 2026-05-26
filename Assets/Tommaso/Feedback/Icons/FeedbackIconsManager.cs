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
        public FeedbackIconController iconController;
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

    // Tiene traccia del WayPointSmall attivo nello step precedente (solo Sequenziale)
    private GameObject previousWaypoint = null;

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

        Debug.Log("[FeedbackIconsManager] Profilo: " + (isVisivo ? "Visivo" : "Verbale"));

        FeedbackIconController[] controllers = FindObjectsByType<FeedbackIconController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var controller in controllers)
            controller.SetMode(isVisivo);
    }

    // ─────────────────────────────────────────────────────────────────
    // WAYPOINT — interfaccia pubblica per ExecutionOrderController
    // La logica Sequenziale/Globale è gestita internamente.
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chiamare su OnStepStarted. Gestisce internamente Sequenziale e Globale.
    /// </summary>
    public void OnStepStarted(string chapterName, string stepName)
    {
        if (IsSequenziale())
            HandleStepStartedSequenziale(chapterName, stepName);
        // In Globale i waypoint si aggiornano solo al cambio capitolo
    }

    /// <summary>
    /// Chiamare su OnChapterStarted. Gestisce internamente Sequenziale e Globale.
    /// </summary>
    public void OnChapterStarted(string chapterName)
    {
        if (!IsSequenziale())
            HandleChapterStartedGlobale(chapterName);
        // In Sequenziale i waypoint si aggiornano step per step
    }

    // ─────────────────────────────────────────────────────────────────
    // WAYPOINT — implementazioni private
    // ─────────────────────────────────────────────────────────────────

    private void HandleStepStartedSequenziale(string chapterName, string stepName)
    {
        GameObject current = GetWaypoint(chapterName, stepName);

        if (current == null)
        {
            Debug.Log($"[FeedbackIconsManager] Nessun waypoint per step '{stepName}' — nessuna modifica.");
            return;
        }

        // Stesso waypoint dello step precedente: lascia tutto invariato
        if (current == previousWaypoint)
        {
            Debug.Log($"[FeedbackIconsManager] Stesso waypoint per step '{stepName}' — nessuna modifica.");
            return;
        }

        if (previousWaypoint != null)
            previousWaypoint.SetActive(false);

        current.SetActive(true);
        previousWaypoint = current;

        Debug.Log($"[FeedbackIconsManager] Waypoint attivato per step '{stepName}': {current.name}");
    }

    private void HandleChapterStartedGlobale(string chapterName)
    {
        DeactivateAllWaypoints();

        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (chapter == null)
        {
            Debug.Log($"[FeedbackIconsManager] Nessun mapping per il capitolo '{chapterName}'.");
            return;
        }

        foreach (var mapping in chapter.steps)
        {
            if (mapping.iconController == null) continue;

            GameObject wp = GetWaypointFromController(mapping.iconController);
            if (wp != null)
            {
                wp.SetActive(true);
                Debug.Log($"[FeedbackIconsManager] Waypoint attivato per step '{mapping.stepName}': {wp.name}");
            }
        }
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

        if (mapping == null || mapping.iconController == null) return;

        // TODO: chiama il metodo corretto su mapping.iconController
        // mapping.iconController.???();
    }

    // ─────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────

    private void DeactivateAllWaypoints()
    {
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("WayPointSmall");
        foreach (GameObject wp in waypoints)
            wp.SetActive(false);

        previousWaypoint = null;
        Debug.Log($"[FeedbackIconsManager] Disattivati {waypoints.Length} waypoint.");
    }

    private GameObject GetWaypoint(string chapterName, string stepName)
    {
        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (chapter == null) return null;

        StepIconMapping mapping = chapter.steps.Find(m =>
            string.Equals(m.stepName, stepName, System.StringComparison.OrdinalIgnoreCase));

        if (mapping == null || mapping.iconController == null) return null;

        return GetWaypointFromController(mapping.iconController);
    }

    private GameObject GetWaypointFromController(FeedbackIconController controller)
    {
        Transform iconFeedback = FindChildWithTag(controller.transform, "IconFeedback");
        if (iconFeedback == null)
        {
            Debug.LogWarning($"[FeedbackIconsManager] Tag 'IconFeedback' non trovato su {controller.gameObject.name}");
            return null;
        }

        Transform waypoint = FindChildWithTag(iconFeedback, "WayPointSmall");
        if (waypoint == null)
        {
            Debug.LogWarning($"[FeedbackIconsManager] Tag 'WayPointSmall' non trovato sotto IconFeedback di {controller.gameObject.name}");
            return null;
        }

        return waypoint.gameObject;
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