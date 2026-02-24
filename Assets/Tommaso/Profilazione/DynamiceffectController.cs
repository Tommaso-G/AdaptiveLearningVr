using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicEffectController : MonoBehaviour
{
    [Header("1️⃣ Oggetti che lampeggiano con emissione crescente")]
    public List<GameObject> emissionObjects = new List<GameObject>();

    [Header("2️⃣ Oggetti che ruotano su se stessi")]
    public List<GameObject> rotatingObjects = new List<GameObject>();

    [Header("3️⃣ Oggetti che tremano")]
    public List<GameObject> shakingObjects = new List<GameObject>();

    [Header("4️⃣ Luci da accendere/spegnere")]
    public List<Light> lights = new List<Light>();

    [Header("Parametri di controllo generale")]
    [Tooltip("Velocità iniziale di lampeggio/rotazione/tremolio.")]
    public float baseSpeed = 1f;

    [Tooltip("Fattore moltiplicativo massimo raggiunto a fine animazione.")]
    public float maxSpeedMultiplier = 5f;

    [Tooltip("Durata totale dell'animazione (in secondi).")]
    public float totalDuration = 10f;

    [Header("Parametri specifici effetti")]
    [Tooltip("Asse di rotazione degli oggetti rotanti.")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Intensità del tremolio (ampiezza).")]
    public float shakeAmount = 0.05f;

    private float elapsedTime;
    private bool effectsRunning = false;

    void Update()
    {
        if (!effectsRunning)
            return;

        elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / totalDuration);
        float currentSpeed = Mathf.Lerp(baseSpeed, baseSpeed * maxSpeedMultiplier, normalizedTime);

        HandleEmissionBlink(currentSpeed);
        HandleRotation(currentSpeed);
        HandleShake(currentSpeed);
        HandleLights(currentSpeed);
    }

    // ===============================
    // METODO PUBBLICO DI AVVIO
    // ===============================
    public void StartEffects()
    {
        if (!effectsRunning)
        {
            StartCoroutine(EffectsRoutine());
        }
    }

    private IEnumerator EffectsRoutine()
    {
        Debug.Log("[DynamicEffectController] Effetti avviati.");
        elapsedTime = 0f;
        effectsRunning = true;

        // Attendi la durata definita
        yield return new WaitForSeconds(totalDuration);

        // Ferma tutto
        StopAllEffects();
        Debug.Log("[DynamicEffectController] Effetti terminati.");
    }

    // ===============================
    // GESTIONE EFFETTI
    // ===============================
    private void HandleEmissionBlink(float speed)
    {
        float blink = Mathf.PingPong(Time.time * speed, 1f);
        foreach (var obj in emissionObjects)
        {
            if (obj == null) continue;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                Color baseColor = Color.white;
                Color emission = baseColor * Mathf.LinearToGammaSpace(blink * 5f);
                renderer.material.SetColor("_EmissionColor", emission);
                DynamicGI.SetEmissive(renderer, emission);
            }
        }
    }

    private void HandleRotation(float speed)
    {
        if (rotationAxis == Vector3.zero)
            return;

        foreach (var obj in rotatingObjects)
        {
            if (obj == null) continue;
            obj.transform.Rotate(rotationAxis.normalized, 100f * speed * Time.deltaTime, Space.Self);
        }
    }

    private void HandleShake(float speed)
    {
        foreach (var obj in shakingObjects)
        {
            if (obj == null) continue;

            float shakeSpeed = speed * 10f;
            Vector3 originalPos = obj.transform.position;

            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount;
            float offsetZ = Mathf.Sin(Time.time * shakeSpeed * 0.8f) * shakeAmount * 0.5f;

            obj.transform.position = originalPos + new Vector3(offsetX, offsetY, offsetZ);
        }
    }

    private void HandleLights(float speed)
    {
        float flicker = Mathf.PingPong(Time.time * speed, 1f);
        bool lightsOn = flicker > 0.5f;

        foreach (var light in lights)
        {
            if (light == null) continue;
            light.enabled = lightsOn;
        }
    }

    // ===============================
    // STOP / RESET
    // ===============================
    private void StopAllEffects()
    {
        effectsRunning = false;

        foreach (var obj in emissionObjects)
        {
            if (obj == null) continue;
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
                DynamicGI.SetEmissive(renderer, Color.black);
            }
        }

        foreach (var light in lights)
        {
            if (light == null) continue;
            light.enabled = false;
        }
    }
}