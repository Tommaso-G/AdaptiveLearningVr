using NUnit.Framework;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;
using VRBuilder.XRInteraction.Interactables;
public class ExitDoor : MonoBehaviour
{
    [Header("Block Door Settings")]
    public bool blocked = false;
    public Sprite BlockedIcon;
    [SerializeField]
    private GameObject secondDoor;

    [Header("HandMenu Settings")]
    [SerializeField] private HandMenuRequester menuRequester;
    public Collider triggerUICollider;
    public CorrectDoorButton correctDoorButton;

    [Header("Feedback Settings")]
    public Transform feedbackPos;
    public FeedbackIconController IconController;

    private bool selected = false;

    private XRKnob interactable;

    [Header("Door Closed Settings")]
    [SerializeField] private float closedAngleThreshold = 5f;
    public bool isDoorClosed = false;

    private Rigidbody BlockedRb;
    private Rigidbody secondDoorRb;

    private Renderer rend;
    private Renderer secondDoorRend;

    private Color[] baseColors;
    private Color renderColor;

    private GameObject mapButton;
    private Transform target;

    public bool canOpenMenu = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rend = GetComponent<Renderer>();
        //secondDoor = transform.Find("secondDoor").GetComponent<Rigidbody>();
        interactable = secondDoor.GetComponentInChildren<XRKnob>(true);
        secondDoorRb = secondDoor.GetComponent<Rigidbody>();
        secondDoorRend = secondDoor.GetComponent<Renderer>();
        BlockedRb = GetComponent<Rigidbody>();
        target = transform.GetChild(0).transform;
        target.transform.parent = null;

        baseColors = new Color[rend.materials.Length];

        for (int i = 0; i < rend.materials.Length; i++)
        {
            baseColors[i] = rend.materials[i].color;
        }

        if (BlockedRb != null)
        {
            isBlock(blocked);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (rend != null)
            changeColorAndIcon();

        // Controlla se le porte sono chiuse
        float exitAngle = transform.localEulerAngles.y;
        float secondAngle = secondDoor.transform.localEulerAngles.y;

        if (exitAngle > 180f) exitAngle -= 360f;
        if (secondAngle > 180f) secondAngle -= 360f;

        isDoorClosed = Mathf.Abs(exitAngle) <= closedAngleThreshold &&
                    Mathf.Abs(secondAngle - 90f) <= closedAngleThreshold;
    }

    private void changeColorAndIcon()
    {
        if (blocked)
        {
            renderColor = Color.red;
            IconController.verbaleText = "Uscita Bloccata";
            IconController.visivoSprite = BlockedIcon;
            IconController.Refresh();
        }
        else if (selected)
        {
            renderColor = Color.green;
        }
        else
        {
            for (int i = 0; i < baseColors.Length; i++)
            {
                var mat = rend.materials[i];
                var secondmat = secondDoorRend.materials[i];
                mat.color = baseColors[i];
                secondmat.color = baseColors[i];
            }
            return;
        }

        for (int i = 0; i < baseColors.Length; i++)
        {
            var mat = rend.materials[i];
            var secondmat = secondDoorRend.materials[i];
            mat.color = Color.Lerp(baseColors[i], renderColor, 0.5f);
            secondmat.color = Color.Lerp(baseColors[i], renderColor, 0.5f);
        }
    }

    public void Select()
    {
        if (!blocked)
        {
            selected = true;
        }
    }

    public void Deselect()
    {
        selected = false;
    }

    public void isBlock(bool blocked)
    {
        BlockedRb.freezeRotation = blocked ? true : false;
        secondDoorRb.freezeRotation = blocked ? true : false;
        selected = blocked ? false : selected;
        target.gameObject.SetActive(!blocked);
        interactable.enabled = blocked ? false : true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            TryOpenMenu();
    }

    public void TryOpenMenu()
    {
        if (!canOpenMenu) return;
        if (!menuRequester.enabled) return;

        menuRequester.OpenMenu();
        correctDoorButton.CallCorrectButton(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!canOpenMenu) return;

        if (other.CompareTag("Player"))
        {
            if (!menuRequester.enabled) return;

            menuRequester.CloseMenu();
        }
    }

}
