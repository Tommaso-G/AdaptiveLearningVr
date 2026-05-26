using System;
using UnityEngine;
using static UnityEngine.XR.Interaction.Toolkit.Gaze.XRGazeAssistance;

using UnityEngine;

public enum InteractionKind
{
    Generic,
    Lever,
    Door,
    Proxy,
    NPC,
    Interactable,
    Trigger,
    UIButton,
    Button
}

public class InteractionData
{
    public InteractionKind kind;

    public GameObject source;

    public GameObject interactor;

    public string errorString;

    public GameObject context;
}

public abstract class InteractionSource : MonoBehaviour
{
    public Action<InteractionData> OnInteraction;

    public virtual bool CanHandle(GameObject obj) => false;

    [SerializeField]
    protected string errorString;

    protected void RaiseInteraction(
        InteractionKind kind,
        GameObject source,
        GameObject interactor = null,
        GameObject context = null
    )
    {
        InteractionData data = new InteractionData
        {
            kind = kind,
            source = source,
            interactor = interactor,
            errorString = errorString,
            context = context
        };

        OnInteraction?.Invoke(data);
    }
}