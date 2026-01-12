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

    [UnityEngine.Range(0, 1)]
    public float intensity;
    public float duration;

    public float UIshowAngle = 30f;
    public float UIhideAngle = 45f;
    private float dotShowThreshold;
    private float dotHideThreshold;
    public float upThreshold = 0.6f;

    [SerializeField] private HapticImpulsePlayer controller;
    [SerializeField] private Transform controllerVisual;
    [SerializeField] private Renderer[] controllerRend;
    [SerializeField] List<Panel> UIPanels;
    [SerializeField] private List<Material> baseMats;
    [SerializeField] GameObject rotationUI;
    private bool isVisible;
    private Animator UIAnimator;
    private bool firstTime;
    private Coroutine rotationCoroutine = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HandMenu.OnOpenPanel += OpenPanel;
        firstTime = true;
        controllerRend = controllerVisual.GetComponentsInChildren<Renderer>(true);
        isVisible = false;
        foreach (Renderer renderer in controllerRend)
        {
            Material[] mats = renderer.materials;
            foreach(Material mat in mats)
            {
                baseMats.Add(mat);
            }
        }
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
        while(k > 0){
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

    void OpenPanel(string panelName, bool open)
    {

        GameObject panel = UIPanels.FirstOrDefault(p => p.key.Equals(panelName))?.panel;
        UIAnimator = panel.GetComponent<Animator>();

        if (open)
        {

            HighlightController();
            TriggerHaptic();
            if(rotationCoroutine == null)
            {
                rotationCoroutine = StartCoroutine(ShowRotationUI());
            }
        }
        else
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
            print("Stop coroutine");
            rotationUI.GetComponent<Animator>().SetBool("ShowRotUI", false);
            UIAnimator = null;

        }
    }

    private  IEnumerator ShowRotationUI()
    {

        Animator animator = rotationUI.GetComponent<Animator>();

        print("firstTime: " + firstTime);
        if (firstTime)
        {
            firstTime = false;
            print("aspetta 2");
            yield return new WaitForSeconds(2f);
            if (isVisible)
            {
                yield return null;
            }
            else
            {
                animator.SetBool("ShowRotUI", true);
                yield return null;
            }
        }
        else
        {

            print("aspetta 10");
            yield return new WaitForSeconds(10f);

            if (isVisible)
            {
                yield return null;
            }
            else
            {
                animator.SetBool("ShowRotUI", true);
            }
        }
    }
    private void UpdateUI()
    {
        if (!controller) return;

        dotShowThreshold = Mathf.Cos((90f - UIshowAngle) * Mathf.Deg2Rad);
        dotHideThreshold = Mathf.Cos((90f - UIhideAngle) * Mathf.Deg2Rad);

        Vector3 controllerForward = controller.transform.forward;
        Vector3 headForward = Camera.main.transform.forward;
        Vector3 headRight = Camera.main.transform.right;

        float dot = Vector3.Dot(controllerForward.normalized, -headForward.normalized);

        float upDot = Vector3.Dot(controller.transform.up.normalized, Vector3.up);

        float sideDot = Vector3.Dot(controllerForward, headRight);


        if (!isVisible && dot < dotShowThreshold && sideDot > 0f && upDot > upThreshold)
        {
            isVisible = true;
            ShowUI();
        }
        else if (isVisible && (Mathf.Abs(dot) > dotHideThreshold || upDot < upThreshold || sideDot < 0f))
        {
            isVisible = false;
            HideUI();
        }
    }

    private void ShowUI()
    {
        rotationUI.GetComponent<Animator>().SetBool("ShowRotUI", false);
        if (UIAnimator != null)
        {
            UIAnimator.SetBool("FadeOut", false);
            UIAnimator.SetBool("FadeIn", true);
            print("FadeIn");
        }
    }

    private void HideUI()
    {
        if (UIAnimator != null)
        {
            UIAnimator.SetBool("FadeIn", false);
            UIAnimator.SetBool("FadeOut", true);
            print("FadeOut");
        }
    }

    public void EndInteraction(GameObject panel)
    {
        panel.SetActive(false);
        StopCoroutine(rotationCoroutine);
        rotationCoroutine = null;
        rotationUI.GetComponent<Animator>().SetBool("ShowRotUI", false);
        UIAnimator = null;
    }

    void LateUpdate()
    {
        if(UIAnimator != null)
        {
            UpdateUI();
        }
    }
}
