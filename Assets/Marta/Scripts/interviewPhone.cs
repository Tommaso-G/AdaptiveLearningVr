using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class interviewPhone : MonoBehaviour
{
    private MeshRenderer renderer;
    private AudioSource audioSource;
    private bool grabbedOnce;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabbedOnce = false;
        renderer = GetComponent<MeshRenderer>();
        audioSource = GetComponentInChildren<AudioSource>();
    }

    public void onGrabbed()
    {
        if (grabbedOnce)
        {
            return;
        }
        grabbedOnce = true;
        audioSource.Play();
        StartCoroutine(WaitForAudio());
    }

    public void onGrabbedEnd()
    {
        HandMenu.OnOpenPanel?.Invoke("Colloquio Pompieri", false);
        renderer.enabled = true;
    }

    private IEnumerator WaitForAudio()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        renderer.enabled = false;
        HandMenu.OnOpenPanel?.Invoke("Colloquio Pompieri", true);
    }
}
