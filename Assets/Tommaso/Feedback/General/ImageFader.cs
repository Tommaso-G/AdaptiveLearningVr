using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageFader : MonoBehaviour
{
    [Header("Immagini")]
    public Image imageOut; // immagine che svanisce
    public Image imageIn;  // immagine che appare

    [Header("Impostazioni")]
    [Min(0.01f)]
    public float fadeDuration = 0.5f;

    private Coroutine fadeCoroutine;

    public void Fade()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // Stato iniziale
        if (imageOut != null)
        {
            imageOut.gameObject.SetActive(true);
            SetAlpha(imageOut, 1f);
        }

        if (imageIn != null)
        {
            imageIn.gameObject.SetActive(true);
            SetAlpha(imageIn, 0f);
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            if (imageOut != null) SetAlpha(imageOut, 1f - t);
            if (imageIn != null)  SetAlpha(imageIn, t);

            yield return null;
        }

        // Stato finale
        if (imageOut != null)
        {
            SetAlpha(imageOut, 0f);
            imageOut.gameObject.SetActive(false);
        }

        if (imageIn != null)
            SetAlpha(imageIn, 1f);

        fadeCoroutine = null;
    }

    private void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}