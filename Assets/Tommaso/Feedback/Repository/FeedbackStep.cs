using UnityEngine;

[System.Serializable]
public class FeedbackStep
{
    [Header("Nome dello step")]
    public string stepName;

    [Header("Prefab associati")]
    public GameObject visivoPrefab;
    public GameObject verbalePrefab;

    /// <summary>
    /// Restituisce il prefab corretto in base alla modalità visiva o verbale.
    /// </summary>
    public GameObject GetByMode(LearningEnums.VisivoVerbale mode)
    {
        return mode == LearningEnums.VisivoVerbale.Visivo ? visivoPrefab : verbalePrefab;
    }
}
