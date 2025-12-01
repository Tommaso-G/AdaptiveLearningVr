using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LearningPath
{


    [Header("Elenco Step per questo percorso")]
    public List<FeedbackStep> steps = new List<FeedbackStep>();

    /// <summary>
    /// Restituisce uno step dato l’indice.
    /// </summary>
    public FeedbackStep GetStep(int index)
    {
        if (index < 0 || index >= steps.Count)
        {
            Debug.LogWarning($"[LearningPath] Indice step {index} non valido ");
            return null;
        }

        return steps[index];
    }
}
