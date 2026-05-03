using System.Collections.Generic;
using UnityEngine;

public class AdaptiveVisualManager : MonoBehaviour
{
    [Header("Riferimento al profilo")]
    public LearningProfile learningProfile;

    [Header("Contenuti")]
    public List<GameObject> visualObjects;
    public List<GameObject> verbalObjects;

    void Start()
    {
        if (learningProfile == null)
        {
            Debug.LogError("LearningProfile non assegnato!");
            return;
        }

        // Disattiva tutto
        SetActiveList(visualObjects, false);
        SetActiveList(verbalObjects, false);

        // Legge il profilo FSLSM
        var profile = learningProfile.GetProfileTuple();

        // Usa SOLO la dimensione Visivo/Verbale
        switch (profile.visivoVerbale)
        {
            case LearningEnums.VisivoVerbale.Visivo:
                SetActiveList(visualObjects, true);
                break;

            case LearningEnums.VisivoVerbale.Verbale:
                SetActiveList(verbalObjects, true);
                break;
        }
    }

    void SetActiveList(List<GameObject> list, bool state)
    {
        foreach (var obj in list)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}