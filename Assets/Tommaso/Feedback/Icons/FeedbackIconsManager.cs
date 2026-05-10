using UnityEngine;

public class FeedbackIconsManager : MonoBehaviour
{
    public LearningProfile learningProfile;

    void Start()
    {
        ApplyLearningStyle();
    }

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

        // Una sola chiamata, con gli inattivi inclusi
        FeedbackIconController[] controllers = FindObjectsByType<FeedbackIconController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log($"[FeedbackIconsManager] Controller trovati: {controllers.Length}");

        foreach (var controller in controllers)
        {
            controller.SetMode(isVisivo);
        }
    }
}