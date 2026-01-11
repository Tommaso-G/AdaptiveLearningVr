using NUnit.Framework;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;
public class ExitDoor : MonoBehaviour
{
    public bool blocked = false;
    public Collider triggerUICollider;
    public CorrectDoorButton correctDoorButton;
    private bool selected = false;
    private Rigidbody BlockedRb;
    private Rigidbody secondDoor;
    private Renderer rend;
    private Renderer secondDoorRend;
    private Color[] baseColors;
    private Color renderColor;
    private GameObject mapButton;
    private Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rend = GetComponent<Renderer>();
        secondDoor = transform.Find("secondDoor").GetComponent<Rigidbody>();
        secondDoorRend = secondDoor.gameObject.transform.GetComponent<Renderer>();
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
        {
            changeColor();
        }

        //if(BlockedRb != null)
        //{
        //    isBlock(blocked);
        //}
    }

    private void changeColor()
    {
        if (blocked)
        {
            renderColor = Color.red;
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

    public void checkState()
    {
        isBlock(blocked);
    }
    public void isBlock(bool blocked)
    {
        BlockedRb.freezeRotation = blocked ? true : false;
        secondDoor.freezeRotation = blocked ? true : false;
        selected = blocked ? false : selected;
        target.gameObject.SetActive(!blocked);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HandMenu.OnOpenPanel?.Invoke("Mappa Vie Di Fuga", true);
            correctDoorButton.CallCorrectButton(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HandMenu.OnOpenPanel?.Invoke("Mappa Vie Di Fuga", false);
        }
    }

}
