using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core;
using VRBuilder.Core.Entities;
using VRBuilder.Core.Properties;
public class InteractionListener : MonoBehaviour
{
    public ExecutionOrderController executionOrderController;
    private VisualProxy visualProxy;
    private string chapter_name;
    private IProcess process;
    private bool hasColliderProperty = false;

    void Start()
    {
        XRBaseInteractable interactable = this.GetComponentInParent<XRBaseInteractable>(true);
        XRPushButtonVrBuilder button = this.GetComponentInParent<XRPushButtonVrBuilder>(true);
        XRLever lever = this.GetComponentInParent<XRLever>(true);
        Button buttonUI = this.GetComponentInParent<Button>(true);
        ClosableDoor door = this.GetComponentInParent<ClosableDoor>(true);
        FollowerAgentWithCheck npc = this.GetComponentInParent<FollowerAgentWithCheck>(true);
        visualProxy = this.GetComponentInParent<VisualProxy>(true);
        ColliderWithTriggerProperty colliderProperty = this.GetComponent<ColliderWithTriggerProperty>();

        if (button != null)
        {
            button.onPress.AddListener(OnButtonPressed);
            Debug.Log("Trovata button property su: " + gameObject.name);
        }
        else if (lever != null)
        {
            lever.onLeverActivate.AddListener(OnLeverActivated);
            Debug.Log("Trovata lever property su: " + gameObject.name);
        }
        else if (buttonUI != null)
        {
            buttonUI.onClick.AddListener(OnButtonUIClicked);
            Debug.Log("Trovata buttonUI property su: " + gameObject.name);
        }
        else if (door != null)
        {
            door.onDoorClosed.AddListener(OnDoorClosed);
            Debug.Log("Trovata door property su: " + gameObject.name);
        }
        else if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnInteraction);
            Debug.Log("Trovata interactable property su: " + gameObject.name);

        }
        else if (npc != null)
        {
            FollowPlayerBehavior.OnFollowPlayerTriggered += onFollowPlayer;
        }
        else if (visualProxy != null)
        {
            visualProxy.OnGrabbed += onGrabbedProxy;
        }else if(colliderProperty != null)
        {
            hasColliderProperty = true;
            Debug.Log("Trovata collider property su: " + gameObject.name);
        }
        else
        {
            Debug.Log("Impossobile attivare listener a: " + gameObject.name);
        }
    }
    public void Initialize(IProcess process)
    {
        this.process = process;
    }

    private void OnDoorClosed()
    {
        chapter_name = process.Data.Current.Data.Name;
        executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name, errorString: "Porta");
        Debug.Log("Interazione con: " + gameObject.name);

    }

    private void OnButtonUIClicked()
    {
        chapter_name = process?.Data.Current?.Data.Name;
        executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void OnInteraction(BaseInteractionEventArgs args)
    {
        chapter_name = process.Data.Current.Data.Name;
        if (args.interactorObject.transform.gameObject.name == "Near-Far Interactor")
        {
            executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name);
            //Debug.Log("Interazione con: " + args.interactableObject.transform.gameObject.name);
        }
    }

    private void OnButtonPressed()
    {
        chapter_name = process.Data.Current.Data.Name;
        executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void OnLeverActivated()
    {
        chapter_name = process.Data.Current.Data.Name;
        executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name);
        //Debug.Log("Interazione con: " + gameObject.name);
    }

    private void onFollowPlayer(GameObject follower, GameObject player)
    {
        chapter_name = process.Data.Current.Data.Name;
        if (follower == this.gameObject)
        {
            executionOrderController.checkForObjInStep(follower, chapter_name: chapter_name, errorString: "Bambino");
            Debug.Log("Interazione con: " + follower.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!hasColliderProperty) return;

        if(other.gameObject.CompareTag("Player"))
        {
            chapter_name = process.Data.Current.Data.Name;
            executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name, errorString: "Aula"); 
        }
    }
    public void onGrabbedProxy()
    {
        chapter_name = process.Data.Current.Data.Name;
        executionOrderController.checkForObjInStep(gameObject, chapter_name: chapter_name, visualProxy.activeproxy);
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
