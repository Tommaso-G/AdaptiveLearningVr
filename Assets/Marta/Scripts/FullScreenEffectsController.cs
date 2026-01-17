using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullScreenEffectsController : MonoBehaviour
{
    [Header("Time Stats")]
    [SerializeField] private float _effectFadeTime = 0.5f;

    [Header("Reference")]
    [SerializeField] private ScriptableRendererFeature _fullScreenEffect;
    [SerializeField] private Material _material;
    [SerializeField] private Volume _cameraVignette;
    [SerializeField] private Animation _animation;

    private int _noiseIntensity = Shader.PropertyToID("_NoiseIntensity");
    private int _vignetteIntensity = Shader.PropertyToID("_VignetteIntensity");

    private const float NOISE_INTENSITY_START_AMOUNT = 1.1f;
    private const float VIGNETTE_INTENSITY_START_AMOUNT = 1.54f;
    private bool canActivate = false;   // controllo esterno
    private bool playerInside = false;  // trigger
    private bool effectActive = false;  // stato reale dell’effetto
    private Coroutine coroutine;


    private void Start()
    {
        _material.SetFloat(_noiseIntensity, 0f);
        _material.SetFloat(_vignetteIntensity, 0f);
        _cameraVignette.weight = 0f;
    }

    public IEnumerator FadeInScreenEffect()
    {
        float elapsedTime = 0f;
        while(elapsedTime < _effectFadeTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpNoise = Mathf.Lerp(0f, NOISE_INTENSITY_START_AMOUNT, (elapsedTime / _effectFadeTime));
            float lerpVignette = Mathf.Lerp(0f, VIGNETTE_INTENSITY_START_AMOUNT, (elapsedTime / _effectFadeTime));
            float lerpCameraVignette = Mathf.Lerp(0f, 1f, (elapsedTime / _effectFadeTime));

            _cameraVignette.weight = lerpCameraVignette;
            _material.SetFloat(_noiseIntensity, lerpNoise);
            _material.SetFloat(_vignetteIntensity, lerpVignette);

            yield return null;
        }

        if (_animation != null)
        {
            _animation.enabled = true;
        }

    }

    public IEnumerator FadeOutScreenEffect()
    {
        if (_cameraVignette.weight != 0f)
        {
            float elapsedTime = 0f;
            while (elapsedTime < _effectFadeTime)
            {
                elapsedTime += Time.deltaTime;

                float lerpNoise = Mathf.Lerp(NOISE_INTENSITY_START_AMOUNT, 0f, (elapsedTime / _effectFadeTime));
                float lerpVignette = Mathf.Lerp(VIGNETTE_INTENSITY_START_AMOUNT, 0f, (elapsedTime / _effectFadeTime));
                float lerpCameraVignette = Mathf.Lerp(1f, 0f, (elapsedTime / _effectFadeTime));

                _cameraVignette.weight = lerpCameraVignette;
                _material.SetFloat(_noiseIntensity, lerpNoise);
                _material.SetFloat(_vignetteIntensity, lerpVignette);

                yield return null;
            }

            if (_animation != null)
            {
                _animation.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        EvaluateEffectState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        EvaluateEffectState();
    }

    private void EvaluateEffectState()
    {
        bool shouldBeActive = canActivate && playerInside;

        if (shouldBeActive == effectActive)
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(
            shouldBeActive ? FadeInScreenEffect() : FadeOutScreenEffect()
        );

        effectActive = shouldBeActive;
    }

    public void SetCanActivate(bool value)
    {
        canActivate = value;
        EvaluateEffectState();
    }
}
