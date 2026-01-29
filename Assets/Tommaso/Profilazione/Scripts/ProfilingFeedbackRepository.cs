using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static FeedbackRepository;

[CreateAssetMenu(fileName = "ProfilingFeedbackRepository", menuName = "VR Feedback/Profiling Repository")]
public class ProfilingFeedbackRepository : ScriptableObject
{
    [Header("Prefab (UI)")]
    [Tooltip("Prefab per feedback con una o più immagini statiche.")]
    public GameObject SingleContainer;

    [Tooltip("Prefab per feedback con un singolo video.")]
    public GameObject MultipleContainer;

    [Header("Capitoli (Profiling)")]
    [Tooltip("Lista semplificata di capitoli e feedback, senza percorsi globali/sequenziali.")]
    public List<Chapter> chapters = new List<Chapter>();


    public FeedbackData GetFeedbackByStep(string chapterName, string stepName)
    {
        if (chapters == null || chapters.Count == 0)
            return null;

        var chapter = chapters.FirstOrDefault(c =>
            string.Equals(c.ChapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

        return chapter?.feedbacks.FirstOrDefault(f => f.StepForCompletition.Contains(stepName));
    }

    public IEnumerable<FeedbackData> GetAllFeedbacks()
    {
        if (chapters == null || chapters.Count == 0)
            return Enumerable.Empty<FeedbackData>();

        return chapters.SelectMany(c => c.feedbacks);
    }
}
