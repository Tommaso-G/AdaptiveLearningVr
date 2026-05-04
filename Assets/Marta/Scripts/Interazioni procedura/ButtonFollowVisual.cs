using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class ButtonFollowVisual : MonoBehaviour
{
    public Vector3 localAxis;
    public Transform visualTarget;
    public float ResetSpeed = 5f;
    public float followAngleTreshold = 45f;
    public float maxPushDistance = 0.02f;

    private bool freeze = false;

    private Vector3 initialLocalPose; 

    private Vector3 offeset;
    private Transform pokeAttachTransform;

    XRBaseInteractable interactable;
    private bool isFollowing; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialLocalPose = visualTarget.localPosition;
        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(Follow);
        interactable.hoverExited.AddListener(Reset);
        interactable.selectEntered.AddListener(Freeze);
    }

    public void Follow(BaseInteractionEventArgs hover)
    {
        if(hover.interactorObject is XRPokeInteractor)
        {
            XRPokeInteractor interactor = (XRPokeInteractor)hover.interactorObject;
            pokeAttachTransform = interactor.attachTransform;
            offeset = visualTarget.position - pokeAttachTransform.position;

            float pokeAngle = Vector3.Angle(offeset, visualTarget.TransformDirection(localAxis));

            if(pokeAngle < followAngleTreshold)
            {
                isFollowing = true;
                freeze = false;
            }
        }
    }

    public void Reset(BaseInteractionEventArgs hover)
    {
        if(hover.interactorObject is XRPokeInteractor)
        {
            isFollowing = false;
            freeze = false;
        }
        
    }

    public void Freeze(BaseInteractionEventArgs hover)
    {
        if (hover.interactorObject is XRPokeInteractor)
        {
            freeze = true;
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (freeze)
        {
            return;
        }

        if (isFollowing) 
        {
            Vector3 localTargetPosition = visualTarget.InverseTransformPoint(pokeAttachTransform.position +  offeset);
            Vector3 constrainedLocalTargetPosition = Vector3.Project(localTargetPosition, localAxis);

            float distance = Vector3.Dot(constrainedLocalTargetPosition, localAxis.normalized);
            distance = Mathf.Clamp(distance, 0.0f, maxPushDistance);

            Vector3 finalePosition = localAxis.normalized * distance;

            visualTarget.localPosition = initialLocalPose + finalePosition;
            //visualTarget.position = visualTarget.TransformPoint(constrainedLocalTargetPosition);
        }
        else
        {
            visualTarget.localPosition = Vector3.Lerp(visualTarget.localPosition, initialLocalPose, Time.deltaTime * ResetSpeed);
        }
    }
}
