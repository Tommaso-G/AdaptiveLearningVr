using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticsUtility;
//using XRControllerInputActionManager =
//Unity.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager;
[System.Serializable]
public class Panel
{
    public string key;
    public GameObject panel;
}
public class HandMenu : MonoBehaviour
{
    public static Action<string, bool> OnOpenPanel;

    [Header("UI show settings")]
    public float UIshowAngle = 30f;
    public float UIhideAngle = 45f;
    public float upThreshold = 0.6f;
    private float dotShowThreshold;
    private float dotHideThreshold;

    [Header("Controller Info")]
    [SerializeField] private HapticImpulsePlayer controller;
    [SerializeField] private Transform controllerVisual;
    [SerializeField] private Renderer[] controllerRend;
    [SerializeField] private List<Material> baseMats;

    [Header("Vibration feedback settings")]
    [UnityEngine.Range(0, 1)]
    public float intensity;
    public float duration;

    [Header("UI Rotation")]
    [SerializeField] GameObject rotationUI;
    private Animator animatorRotUI;
    private bool firstTime;
    private Coroutine rotationCoroutine = null;

    [System.Serializable]
    public class MenuEntry
    {
        public string id;
        public HandMenuPanel panel;
    }


    [Header("Hand Menu Panels")]
    [SerializeField] private List<MenuEntry> menus;

    private Dictionary<string, HandMenuPanel> menuMap;
    private HandMenuPanel currentMenu;

    private bool isMenuActive = false;
    private bool isVisible;
    private float lastToggleTime = 0f;
    private float minToggleInterval = 0.2f;
    private float sideDotMargin = 0.05f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        menuMap = new Dictionary<string, HandMenuPanel>();
        foreach (var m in menus)
        {
            if (!menuMap.ContainsKey(m.id))
                menuMap.Add(m.id, m.panel);
        }
    }
    void Start()
    {
        controllerRend = controllerVisual.GetComponentsInChildren<Renderer>(true);

        isVisible = false;

        animatorRotUI = rotationUI.GetComponent<Animator>();
        firstTime = true;

        foreach (Renderer renderer in controllerRend)
        {
            Material[] mats = renderer.materials;
            foreach (Material mat in mats)
            {
                baseMats.Add(mat);
            }
        }
    }
    public void RequestOpen(string menuId)
    {
        if (!menuMap.ContainsKey(menuId)) // il menù è presente in elenco
            return;

        if (currentMenu != null && currentMenu.MenuId != menuId) // non si sovrappongo due richieste diverse di apertura
            return;

        if (currentMenu == null) // apri il menù
        {
            currentMenu = menuMap[menuId]; // seleziona il Menu panel
            OnMenuOpened();
        }
    }

    public void RequestClose(string menuId)
    {
        if (currentMenu == null)
            return;

        if (currentMenu.MenuId != menuId)
            return;

        CloseCurrent();
    }

    public void CloseCurrent()
    {
        if (currentMenu == null)
            return;

        print("CLOSE REQUEST ACCEPTED");
        currentMenu.Close();
        currentMenu = null;
        OnMenuClosed();
    }

    private void OnMenuOpened()
    {
        print("OPEN REQUEST ACCEPTED");
        TriggerHaptic();
        HighlightController();
        if (rotationCoroutine == null)
        {
            rotationCoroutine = StartCoroutine(ShowRotationUI());
        }
        isMenuActive = true;
    }

    private void OnMenuClosed()
    {
        isMenuActive = false;
        isVisible = false;
        HideRotationUI();
    }
    private void TriggerHaptic()
    {
        if (intensity > 0)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }
    private void HighlightController()
    {
        StartCoroutine(blinkColor(baseMats));
    }

    private IEnumerator blinkColor(List<Material> mats)
    {
        int k = 3;
        while (k > 0)
        {
            foreach (Material mat in mats)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", mat.color * 2f);
            }
            yield return new WaitForSeconds(0.5f);
            foreach (Material mat in mats)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", mat.color * 0.5f);
            }
            yield return new WaitForSeconds(0.5f);
            k--;
        }
        foreach (Material mat in mats)
        {
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
        }
    }

    private IEnumerator ShowRotationUI()
    {

        print("SHOW ROT UI STARTED");
        if (firstTime)
        {
            firstTime = false;

            yield return new WaitForSeconds(2f);
            if (isVisible)
            {
                yield return null;
            }
            else
            {
                animatorRotUI.SetBool("ShowRotUI", true);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(10f);

            if (isVisible)
            {
                yield return null;
            }
            else
            {
                animatorRotUI.SetBool("ShowRotUI", true);
            }
        }
    }

    private void HideRotationUI()
    {

        print("HIDE ROT UI STARTED");
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
        animatorRotUI.SetBool("ShowRotUI", false);
    }

    private void UpdateUI()
    {
        if (!controller || currentMenu == null)
            return;

        float now = Time.time;

        // Calcola le soglie in dot
        dotShowThreshold = Mathf.Cos((90f - UIshowAngle) * Mathf.Deg2Rad);
        dotHideThreshold = Mathf.Cos((90f - UIhideAngle) * Mathf.Deg2Rad);

        Vector3 controllerForward = controller.transform.forward;
        Vector3 controllerUp = controller.transform.up;
        Vector3 headForward = Camera.main.transform.forward;
        Vector3 headRight = Camera.main.transform.right;

        float dot = Vector3.Dot(controllerForward.normalized, -headForward.normalized);
        float upDot = Vector3.Dot(controllerUp.normalized, Vector3.up);
        float sideDot = Vector3.Dot(controllerForward, headRight);

        // Applica cooldown
        if (now - lastToggleTime < minToggleInterval)
            return;

        // Controllo apertura menu
        if (!isVisible &&
            dot < dotShowThreshold &&
            sideDot > 0f + sideDotMargin &&
            upDot > upThreshold)
        {
            isVisible = true;
            HideRotationUI();
            currentMenu.Open();
            lastToggleTime = now;
            print("OPENING MENU");
        }
        // Controllo chiusura menu
        else if (isVisible &&
            (dot > dotHideThreshold ||
             sideDot < 0f - sideDotMargin ||
             upDot < upThreshold))
        {
            isVisible = false;
            currentMenu.Close();
            lastToggleTime = now;
            print("CLOSING MENU");
        }
    }

    void LateUpdate()
    {
        if (isMenuActive)
        {
            UpdateUI();
        }
    }
}
