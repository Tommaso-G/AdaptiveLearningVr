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

        // Trova tutti gli script in scena
        FeedbackIconController[] controllers = FindObjectsByType<FeedbackIconController>(FindObjectsSortMode.None);

        foreach (var controller in controllers)
        {
            controller.SetMode(isVisivo);
        }
    }
}