using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeedbackRepository", menuName = "VR Feedback/Repository Hierarchical")]
public class FeedbackRepository : ScriptableObject
{
    [Header("Tutti i capitoli del training")]
    public List<FeedbackChapter> chapters = new List<FeedbackChapter>();

    /// <summary>
    /// Restituisce il prefab corretto in base al profilo dell'utente, al capitolo e allo step.
    /// </summary>
    public GameObject GetFeedbackPrefab(LearningProfile profile, int chapterIndex, int stepIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Count)
        {
            Debug.LogWarning("[FeedbackRepository] Capitolo non valido.");
            return null;
        }

        var chapter = chapters[chapterIndex];
        var path = chapter.GetPath(profile.sequenzialeGlobale);
        if (path == null)
        {
            Debug.LogWarning($"[FeedbackRepository] Nessun percorso trovato per {profile.sequenzialeGlobale} in {chapter.chapterName}");
            return null;
        }

        var step = path.GetStep(stepIndex);
        if (step == null)
        {
            Debug.LogWarning($"[FeedbackRepository] Nessuno step {stepIndex} ");
            return null;
        }

        return step.GetByMode(profile.visivoVerbale);
    }
}
