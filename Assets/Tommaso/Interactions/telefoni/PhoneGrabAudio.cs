using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.XRInteraction.Properties;
using VRBuilder.BasicInteraction.Properties;
using System.Collections;

public class GrabAudioSelector : MonoBehaviour, ICompletableStep
{
    [Header("Interactable Buttons")]
    [SerializeField] private XRSimpleInteractable buttonA;
    [SerializeField] private XRSimpleInteractable buttonB;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clipA;
    [SerializeField] private AudioClip clipB;

    [Header("Audio Duration Required (seconds)")]
    [SerializeField] private float requiredDuration = 9f;

    private AudioSource audioSource;
    private GrabbableProperty grabbable;

    private enum SelectedMode { None, A, B }
    private SelectedMode currentMode = SelectedMode.None;

    private enum Phase { WaitingForA, WaitingForUnlock, WaitingForB, Done }
    private Phase currentPhase = Phase.WaitingForA;

    public bool IsCompleted { get; private set; } = false;

    private Coroutine audioCheckCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        grabbable = GetComponent<GrabbableProperty>();
    }

    private void OnEnable()
    {
        buttonA.selectEntered.AddListener(OnButtonASelected);
        buttonB.selectEntered.AddListener(OnButtonBSelected);
        grabbable.GrabStarted.AddListener(OnGrabbed);
        grabbable.GrabEnded.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        buttonA.selectEntered.RemoveListener(OnButtonASelected);
        buttonB.selectEntered.RemoveListener(OnButtonBSelected);
        grabbable.GrabStarted.RemoveListener(OnGrabbed);
        grabbable.GrabEnded.RemoveListener(OnReleased);
    }

    private void OnButtonASelected(SelectEnterEventArgs args)
    {
        currentMode = SelectedMode.A;
    }

    private void OnButtonBSelected(SelectEnterEventArgs args)
    {
        currentMode = SelectedMode.B;
    }

    private void OnGrabbed(GrabbablePropertyEventArgs args)
    {
        if (audioCheckCoroutine != null)
            StopCoroutine(audioCheckCoroutine);

        if (currentPhase == Phase.WaitingForA && currentMode == SelectedMode.A && clipA != null)
        {
            audioSource.PlayOneShot(clipA);
            audioCheckCoroutine = StartCoroutine(CheckAudioDuration(requiredDuration, OnPhaseACompleted));
        }
        else if (currentPhase == Phase.WaitingForB && currentMode == SelectedMode.B && clipB != null)
        {
            audioSource.PlayOneShot(clipB);
            audioCheckCoroutine = StartCoroutine(CheckAudioDuration(requiredDuration, OnPhaseBCompleted));
        }
    }

    private void OnReleased(GrabbablePropertyEventArgs args)
    {
        if (audioCheckCoroutine != null)
        {
            StopCoroutine(audioCheckCoroutine);
            audioCheckCoroutine = null;
        }
    }

    private IEnumerator CheckAudioDuration(float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        onComplete?.Invoke();
    }

    private void OnPhaseACompleted()
    {
        Debug.Log("[GrabAudioSelector] Fase A completata.");
        IsCompleted = true;
        currentPhase = Phase.WaitingForUnlock;
        currentMode = SelectedMode.None;
        StartCoroutine(ResetIsCompletedAfterFrame());
    }

    private IEnumerator ResetIsCompletedAfterFrame()
    {
        yield return null;
        IsCompleted = false;
    }

    /// <summary>
    /// Chiama questo metodo dall'esterno per sbloccare la fase B.
    /// </summary>
    public void UnlockPhaseB()
    {
        if (currentPhase != Phase.WaitingForUnlock)
        {
            Debug.LogWarning("[GrabAudioSelector] UnlockPhaseB chiamato fuori sequenza, ignorato.");
            return;
        }

        currentPhase = Phase.WaitingForB;
        currentMode = SelectedMode.None;
        Debug.Log("[GrabAudioSelector] Fase B sbloccata.");
    }

    private void OnPhaseBCompleted()
    {
        Debug.Log("[GrabAudioSelector] Fase B completata. IsCompleted = true definitivo.");
        IsCompleted = true;
        currentPhase = Phase.Done;
    }
}