using UnityEngine;

[System.Serializable]
public class FeedbackChapter
{
    [Header("Nome capitolo")]
    public string chapterName;

    [Header("Percorsi di apprendimento")]
    public LearningPath globale;
    public LearningPath sequenziale;

    /// <summary>
    /// Restituisce il percorso corrispondente (Globale o Sequenziale).
    /// </summary>
    public LearningPath GetPath(LearningEnums.SequenzialeGlobale type)
    {
        return type == LearningEnums.SequenzialeGlobale.Globale ? globale : sequenziale;
    }
}
