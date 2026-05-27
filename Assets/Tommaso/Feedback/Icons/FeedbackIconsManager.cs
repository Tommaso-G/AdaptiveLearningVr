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

    // Tiene traccia dei WayPointSmall attivi nello step precedente (solo Sequenziale)
    private List<GameObject> previousWaypoints = new List<GameObject>();

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
    // ─────────────────────────────────────────────────────────────────

    public void OnStepStarted(string chapterName, string stepName)
    {
        if (IsSequenziale())
            HandleStepStartedSequenziale(chapterName, stepName);
    }

    public void OnChapterStarted(string chapterName)
    {
        if (!IsSequenziale())
            HandleChapterStartedGlobale(chapterName);
    }

    // ─────────────────────────────────────────────────────────────────
    // WAYPOINT — implementazioni private
    // ─────────────────────────────────────────────────────────────────

    private void HandleStepStartedSequenziale(string chapterName, string stepName)
    {
        List<GameObject> current = GetWaypoints(chapterName, stepName);

        if (current == null || current.Count == 0)
        {
            Debug.Log($"[FeedbackIconsManager] Nessun waypoint per step '{stepName}' — nessuna modifica.");
            return;
        }

        // Se i waypoint sono gli stessi del precedente, lascia tutto invariato
        if (SameWaypoints(current, previousWaypoints))
        {
            Debug.Log($"[FeedbackIconsManager] Stessi waypoint per step '{stepName}' — nessuna modifica.");
            return;
        }

        // Disattiva i precedenti
        foreach (GameObject wp in previousWaypoints)
            if (wp != null) wp.SetActive(false);

        // Attiva i correnti
        foreach (GameObject wp in current)
            if (wp != null) wp.SetActive(true);

        previousWaypoints = current;

        Debug.Log($"[FeedbackIconsManager] {current.Count} waypoint attivati per step '{stepName}'.");
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
            foreach (var controller in mapping.iconControllers)
            {
                if (controller == null) continue;
                GameObject wp = GetWaypointFromController(controller);
                if (wp != null)
                {
                    wp.SetActive(true);
                    Debug.Log($"[FeedbackIconsManager] Waypoint attivato per step '{mapping.stepName}': {wp.name}");
                }
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

        if (mapping == null || mapping.iconControllers.Count == 0) return;

        // TODO: chiama il metodo corretto su ogni controller
        // foreach (var controller in mapping.iconControllers)
        //     controller.???();
    }

    // ─────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────

    private void DeactivateAllWaypoints()
    {
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("WayPointSmall");
        foreach (GameObject wp in waypoints)
            wp.SetActive(false);

        previousWaypoints.Clear();
        Debug.Log($"[FeedbackIconsManager] Disattivati {waypoints.Length} waypoint.");
    }

    private List<GameObject> GetWaypoints(string chapterName, string stepName)
    {
        ChapterStepIconMapping chapter = chapterStepMappings.Find(c =>
            string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        if (chapter == null) return null;

        StepIconMapping mapping = chapter.steps.Find(m =>
            string.Equals(m.stepName, stepName, System.StringComparison.OrdinalIgnoreCase));

        if (mapping == null) return null;

        List<GameObject> result = new List<GameObject>();
        foreach (var controller in mapping.iconControllers)
        {
            if (controller == null) continue;
            GameObject wp = GetWaypointFromController(controller);
            if (wp != null)
                result.Add(wp);
        }
        return result;
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

    private bool SameWaypoints(List<GameObject> a, List<GameObject> b)
    {
        if (a.Count != b.Count) return false;
        foreach (GameObject wp in a)
            if (!b.Contains(wp)) return false;
        return true;
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