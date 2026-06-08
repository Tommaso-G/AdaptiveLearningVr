using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMaterialAndLightController : MonoBehaviour
{
    [Header("Target — Luci e Materiale primario")]
    [Tooltip("Root da cui cercare PointLight/SpotLight e Renderer. Se null, usa questo GameObject.")]
    public GameObject targetRoot;

    [Header("Target — Materiale secondario")]
    [Tooltip("Root aggiuntivo i cui figli Renderer riceveranno secondaryMaterial.")]
    public GameObject secondaryRoot;

    [Header("Material Settings")]
    [Tooltip("Materiale da applicare ai Renderer figli di targetRoot.")]
    public Material replacementMaterial;

    [Tooltip("Materiale da applicare ai Renderer figli di secondaryRoot.")]
    public Material secondaryMaterial;

    [Header("Directional Light")]
    [Tooltip("Directional light da spegnere quando viene chiamato Stoplights.")]
    public Light directionalLight;

    [Header("Light Settings")]
    [Tooltip("Intensità idle delle PointLight/SpotLight (stato di riposo).")]
    public float idleIntensity = 0f;

    [Header("Light Flicker — Dying Light")]
    [Tooltip("Durata totale dell'animazione in secondi.")]
    public float flickerDuration = 2.5f;

    [Tooltip("Quanto velocemente decade l'intensità massima raggiungibile (0 = lineare, 1 = molto rapido).")]
    [Range(0f, 1f)]
    public float decayExponent = 0.6f;

    [Tooltip("Durata minima di un singolo burst ON (secondi).")]
    public float burstOnMin = 0.04f;

    [Tooltip("Durata massima di un singolo burst ON (secondi).")]
    public float burstOnMax = 0.18f;

    [Tooltip("Durata minima del buio tra burst (secondi).")]
    public float burstOffMin = 0.03f;

    [Tooltip("Durata massima del buio tra burst (secondi).")]
    public float burstOffMax = 0.25f;

    [Tooltip("Probabilità che un burst sia seguito da un altro burst rapido.")]
    [Range(0f, 1f)]
    public float doubleFlickerChance = 0.35f;

    private Dictionary<Light, float> _originalIntensities = new();

    // -----------------------------------------------------------------------

    public void Stoplights()
    {
        GameObject root = targetRoot != null ? targetRoot : gameObject;
        ApplyMaterialToRenderers(root, replacementMaterial);
        if (secondaryRoot != null)
            ApplyMaterialToRenderers(secondaryRoot, secondaryMaterial);
        if (directionalLight != null)
            directionalLight.enabled = false;
        CacheOriginalIntensities(root);
        PlayLightFlicker();
    }

    public void PlayLightFlicker()
    {
        GameObject root = targetRoot != null ? targetRoot : gameObject;
        Light[] lights = root.GetComponentsInChildren<Light>(includeInactive: true);

        foreach (Light l in lights)
        {
            if (l.type != LightType.Point && l.type != LightType.Spot) continue;
            float delay = Random.Range(0f, 0.12f);
            StartCoroutine(FlickerLight(l, delay));
        }
    }

    // -----------------------------------------------------------------------

    void ApplyMaterialToRenderers(GameObject root, Material mat)
    {
        if (mat == null)
        {
            Debug.LogWarning($"[{nameof(ObjectMaterialAndLightController)}] Nessun materiale assegnato per {root.name}.", this);
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer r in renderers)
        {
            if (r is not MeshRenderer && r is not SkinnedMeshRenderer) continue;
            if (r.sharedMaterials == null || r.sharedMaterials.Length == 0) continue;

            Material[] newMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++)
                newMats[i] = mat;

            r.sharedMaterials = newMats;
        }
    }

    void CacheOriginalIntensities(GameObject root)
    {
        Light[] lights = root.GetComponentsInChildren<Light>(includeInactive: true);
        foreach (Light l in lights)
        {
            if (l.type != LightType.Point && l.type != LightType.Spot) continue;
            _originalIntensities[l] = l.intensity;
        }
    }

    IEnumerator FlickerLight(Light light, float startDelay)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float originalIntensity = _originalIntensities.TryGetValue(light, out float orig) ? orig : light.intensity;

        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            float t = elapsed / flickerDuration;

            float decay = Mathf.Pow(1f - t, decayExponent);
            float currentPeak = Mathf.Lerp(idleIntensity, originalIntensity, decay);

            // --- burst ON ---
            float burstIntensity = currentPeak * Random.Range(0.4f, 1f);
            float onDuration = Random.Range(burstOnMin, burstOnMax);
            float onElapsed = 0f;

            while (onElapsed < onDuration)
            {
                light.intensity = burstIntensity * Random.Range(0.85f, 1.15f);
                float step = Random.Range(0.01f, 0.035f);
                yield return new WaitForSeconds(step);
                onElapsed += step;
                elapsed += step;
            }

            // --- burst OFF ---
            light.intensity = idleIntensity;

            float offMax = Mathf.Lerp(burstOffMax, burstOffMax * 3f, t);
            float offDuration = Random.Range(burstOffMin, offMax);

            // Doppio sfarfallio
            if (Random.value < doubleFlickerChance && t < 0.85f)
            {
                float microOn = Random.Range(0.02f, 0.06f);
                yield return new WaitForSeconds(microOn);
                elapsed += microOn;
                light.intensity = currentPeak * Random.Range(0.2f, 0.6f);
                yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
                elapsed += 0.04f;
                light.intensity = idleIntensity;
            }

            yield return new WaitForSeconds(offDuration);
            elapsed += offDuration;
        }

        light.intensity = idleIntensity;
    }
}