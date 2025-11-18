using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.UI;
public class ExitDoor : MonoBehaviour
{
    [SerializeField] private bool blocked = false;
    private bool selected = false;
    private Rigidbody BlockedRb;
    private Renderer rend;
    private Color[] baseColors;
    private Color renderColor;
    private GameObject mapButton;
    private Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<Renderer>();
        BlockedRb = GetComponent<Rigidbody>();
        mapButton = transform.GetChild(0).GameObject();
        target = transform.GetChild(1).transform;
        target.transform.parent = null;

        baseColors = new Color[rend.materials.Length];

        for (int i = 0; i < rend.materials.Length; i++)
        {
            baseColors[i] = rend.materials[i].color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (rend != null)
        {
            changeColor();
        }

        if(BlockedRb != null)
        {
            isBlock(blocked);
        }
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
                mat.color = baseColors[i];
            }
            return;
        }

            for (int i = 0; i < baseColors.Length; i++)
            {
                var mat = rend.materials[i];
                mat.color = Color.Lerp(baseColors[i], renderColor, 0.5f);
            }
    }

    public void Select()
    {
        if(!blocked)
        {
            selected = true;
        }
    }

    public void Deselect()
    {
        selected = false;
    }

    private void isBlock(bool blocked)
    {
        BlockedRb.freezeRotation = blocked ? true : false;
        selected = blocked ? false : selected;
        mapButton.SetActive(blocked);
        target.gameObject.SetActive(!blocked);
    }
}
