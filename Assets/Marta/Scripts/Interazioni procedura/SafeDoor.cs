using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SafeDoor : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; }
    public MeshRenderer redLight;
    public MeshRenderer greenLight;
    public Material red;
    public Material green;
    public int password;
    public float jointMinLimit = 0f;
    public float jointMaxLimit = 0f;
    public AudioClip wrong;
    public AudioClip correct;

    [SerializeField] private List<XRSimpleInteractable> buttons; // assegna i pulsanti dall'Inspector

    private AudioSource AudioFeedback;
    private string stringPassword;
    private bool ischecking = false;
    private string psw = "";
    private int length;
    private int insertedDigits = 0;
    private Material startLightMat;
    private HingeJoint hinge;

    public void Start()
    {
        stringPassword = password.ToString();
        AudioFeedback = GetComponent<AudioSource>();
        hinge = GetComponent<HingeJoint>();
        startLightMat = redLight.material;
        length = stringPassword.Length;
    }

    public void insertDigit(string n)
    {
        if (ischecking) return;

        if (insertedDigits < length)
        {
            insertedDigits += 1;
            psw += n;

            if (insertedDigits == length)
            {
                CheckPassword();
            }
        }
    }

    public void CheckPassword()
    {
        ischecking = true;
        if (stringPassword == psw)
        {
            greenLight.material = green;

            JointLimits limits = hinge.limits;
            limits.min = jointMinLimit;
            limits.max = jointMaxLimit;
            hinge.limits = limits;
            hinge.useLimits = true;

            AudioFeedback.clip = correct;
            AudioFeedback.Play();
            IsCompleted = true;
        }
        else
        {
            StartCoroutine(ResetPad());
            redLight.material = red;

            JointLimits limits = hinge.limits;
            limits.min = 0f;
            limits.max = 0f;
            hinge.limits = limits;
            hinge.useLimits = true;

            AudioFeedback.clip = wrong;
            AudioFeedback.Play();

            StartCoroutine(ResetPad());
        }
    }

    private IEnumerator ResetPad()
    {
        SetButtonsEnabled(false);
        yield return new WaitForSeconds(2);
        redLight.material = startLightMat;
        psw = "";
        insertedDigits = 0;
        ischecking = false;
        SetButtonsEnabled(true);
    }

    private void AutoCompletition()
    {
        psw = stringPassword;
        CheckPassword();
    }
    private void SetButtonsEnabled(bool enabled)
    {
        foreach (var button in buttons)
            button.enabled = enabled;
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            AutoCompletition();
        }
    }
}