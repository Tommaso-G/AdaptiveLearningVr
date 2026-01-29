using UnityEngine;

public class FeedbackSetHolder : MonoBehaviour
{
    
    public FeedbackRepository FeedbackRepository;
    
    public ProfilingFeedbackRepository ProfilingFeedbackRepository;

    [HideInInspector]
    public GameObject activeFeedbackInstance;

    private void OnValidate()
    {
    if (FeedbackRepository != null && ProfilingFeedbackRepository != null)
    {
        Debug.LogWarning("[FeedbackSetHolder] Assegnati entrambi i repository! Ne verrà usato solo uno (Profiling ha priorità).");
    }
    }

}