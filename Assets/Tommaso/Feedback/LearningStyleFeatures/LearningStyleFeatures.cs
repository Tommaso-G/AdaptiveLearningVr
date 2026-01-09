using UnityEngine;
using VRBuilder.Core;

public abstract class LearningStyleFeatures : ScriptableObject
{
    public abstract void OnFeedbackOpened(FeedbackPrefabController feedback);
    public abstract void OnFeedbackClosed(FeedbackPrefabController feedback);
    public abstract void OnStepActivated(IStep step);
    public abstract void OnStepCompleted(IStep step);
}
