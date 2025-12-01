using UnityEngine;

public class FeedbackSetHolder : MonoBehaviour
{
    public FeedbackRepository Repository;
    
    [HideInInspector]
    public GameObject activeFeedbackInstance;
}