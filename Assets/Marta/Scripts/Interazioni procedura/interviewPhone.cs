using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using VRBuilder.BasicInteraction.Editor.UI.MenuItems;

public class interviewPhone : MonoBehaviour
{
    private MeshRenderer renderer;
    private AudioSource audioSource;
    private bool grabbedOnce;
    [SerializeField] private HandMenuRequester menuRequester;
    [SerializeField] private QuizManager quizManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabbedOnce = false;
        renderer = GetComponent<MeshRenderer>();
        audioSource = GetComponentInChildren<AudioSource>();
        quizManager.OnEnd.AddListener(onGrabbedEnd);
    }

    public void onGrabbed()
    {
        if (grabbedOnce)
        {
            return;
        }
        grabbedOnce = true;
        audioSource?.Play();
        StartCoroutine(WaitForAudio());
    }

    public void onGrabbedEnd()
    {
        menuRequester.CloseMenu();
        renderer.enabled = true;
    }

    private IEnumerator WaitForAudio()
    {
        if (audioSource == null)
            yield break;

        yield return new WaitWhile(() => audioSource.isPlaying);
        renderer.enabled = false;
        menuRequester.OpenMenu();
    }
}
