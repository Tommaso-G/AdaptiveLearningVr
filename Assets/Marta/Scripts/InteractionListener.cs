using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractionListener : MonoBehaviour
{
    private GameObject lastInteractable;
    public ExecutionOrderController executionOrderController;
    private VisualProxy visualProxy;

    void Start()
    {
        lastInteractable = null;
        XRBaseInteractable interactable = this.GetComponentInParent<XRBaseInteractable>(true);
        XRPushButtonVrBuilder button = this.GetComponentInParent<XRPushButtonVrBuilder>(true);
        XRLever lever = this.GetComponentInParent<XRLever>(true);
        Button buttonUI = this.GetComponentInParent<Button>(true);
        ClosableDoor door = this.GetComponentInParent<ClosableDoor>(true);
        FollowerAgentWithCheck npc = this.GetComponentInParent<FollowerAgentWithCheck>(true);
        visualProxy = this.GetComponentInParent<VisualProxy>(true);


        if (button != null)
        {
            button.onPress.AddListener(OnButtonPressed);
        }
        else if (lever != null)
        {
            lever.onLeverActivate.AddListener(OnLeverActivated);
        }
        else if (buttonUI != null)
        {
            buttonUI.onClick.AddListener(OnButtonUIClicked);
        }
        else if (door != null)
        {
            door.onDoorClosed.AddListener(OnDoorClosed);
        }
        else if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnInteraction);

        }
        else if (npc != null)
        {
            FollowPlayerBehavior.OnFollowPlayerTriggered += onFollowPlayer;
        }
        else if (visualProxy != null)
        {
            visualProxy.OnGrabbed += onGrabbedProxy;
        }
        else
        {
            Debug.Log("Impossobile attivare listener a: " + gameObject.name);
        }
    }

    private void OnDoorClosed()
    {
        executionOrderController.checkForObjInStep(gameObject);
        Debug.Log("Interazione con: " + gameObject.name);

    }

    private void OnButtonUIClicked()
    {
        executionOrderController.checkForObjInStep(gameObject);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void OnInteraction(BaseInteractionEventArgs args)
    {
        if (args.interactorObject.transform.gameObject.name == "Near-Far Interactor")
        {
            executionOrderController.checkForObjInStep(gameObject);
            //Debug.Log("Interazione con: " + args.interactableObject.transform.gameObject.name);
        }
    }

    private void OnButtonPressed()
    {
        executionOrderController.checkForObjInStep(gameObject);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void OnLeverActivated()
    {
        executionOrderController.checkForObjInStep(gameObject);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void onFollowPlayer(GameObject follower, GameObject player)
    {
        if (follower == this.gameObject)
        {
            executionOrderController.checkForObjInStep(follower);
            Debug.Log("Interazione con: " + follower.name);
        }
    }

    public void onGrabbedProxy()
    {
        executionOrderController.checkForObjInStep(gameObject, visualProxy.activeproxy);
    }
    private void OnDisable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered -= onFollowPlayer;
        if (visualProxy != null)
        {
            visualProxy.OnGrabbed -= onGrabbedProxy;
        }
    }

}
