using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicEffectController : MonoBehaviour
{
    [Header("Oggetti che lampeggiano con emissione crescente")]
    public List<GameObject> emissionObjects = new List<GameObject>();

    [Header("Oggetti che ruotano su se stessi")]
    public List<GameObject> rotatingObjects = new List<GameObject>();

    [Header("Oggetti che tremano")]
    public List<GameObject> shakingObjects = new List<GameObject>();

    [Header("Luci da accendere/spegnere")]
    public List<Light> lights = new List<Light>();

    [Header("Suono")]
    [Tooltip("Trascina qui l'AudioSource con il suono da modulare.")]
    public AudioSource audioSource;
    [Tooltip("Volume massimo raggiunto al picco.")]
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Header("Oggetto/i che si attivano alla fine")]
    [Tooltip("Oggetto che verrà attivato alla fine e scalerà fino alla scala originale.")]
    public GameObject objectToActivate;
    [Tooltip("Durata dello scaling dell'oggetto finale.")]
    public float scaleDuration = 1f;

    [Header("Oggetti che si rimpiccioliscono e disattivano all'inizio")]
    [Tooltip("Lista di oggetti che si ridurranno e verranno disattivati all'inizio.")]
    public List<GameObject> objectsToShrink = new List<GameObject>();
    [Tooltip("Durata del rimpicciolimento iniziale.")]
    public float shrinkDuration = 1f;

    [Header("Parametri tremolio oggetto finale")]
    [Tooltip("Durata del tremolio dell'oggetto finale in secondi.")]
    public float finalShakeDuration = 25f;
    [Tooltip("Ampiezza del tremolio dell'oggetto finale.")]
    public float finalShakeIntensity = 0.001f;

    [Header("Parametri generali")]
    [Tooltip("Velocità base iniziale di lampeggio/rotazione/tremolio.")]
    public float baseSpeed = 1f;

    [Tooltip("Fattore massimo di moltiplicazione della velocità al picco.")]
    public float speedMultiplier = 3f;

    [Tooltip("Durata totale dell'effetto in secondi.")]
    public float effectDuration = 5f;

    [Tooltip("Asse di rotazione (locale) per gli oggetti rotanti.")]
    public Vector3 rotationAxis = Vector3.up;

    private float elapsedTime;
    private bool effectsActive = false;
    private Coroutine effectCoroutine;

    // Posizioni originali per shake
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    // Scale originali per oggetti
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Vector3 finalObjectOriginalScale;

    void Awake()
    {
        if (objectToActivate != null)
            finalObjectOriginalScale = objectToActivate.transform.localScale;

        foreach (var obj in objectsToShrink)
        {
            if (obj != null && !originalScales.ContainsKey(obj))
                originalScales[obj] = obj.transform.localScale;
        }
    }

    void Update()
    {
        if (!effectsActive) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / effectDuration);

        // Curva smooth che accelera e poi decelera (ease-in-out)
        float intensity = Mathf.SmoothStep(0f, 1f, Mathf.Sin(t * Mathf.PI));

        // Calcola la velocità attuale in base alla curva
        float currentSpeed = baseSpeed * (1f + (speedMultiplier - 1f) * intensity);

        // Aggiorna effetti visivi e audio
        HandleEmissionBlink(currentSpeed);
        HandleRotation(currentSpeed);
        HandleShake(currentSpeed, intensity);
        HandleLights(currentSpeed);
        HandleAudio(intensity);
    }

    // ===========================================================
    // FUNZIONE PUBBLICA DI AVVIO
    // ===========================================================
    [ContextMenu("▶ Avvia effetti")]
    public void PlayAllEffects()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        // salva posizioni e scale originali
        originalPositions.Clear();
        foreach (var obj in shakingObjects)
        {
            if (obj != null && !originalPositions.ContainsKey(obj))
                originalPositions[obj] = obj.transform.position;
        }

        foreach (var obj in objectsToShrink)
        {
            if (obj != null && !originalScales.ContainsKey(obj))
                originalScales[obj] = obj.transform.localScale;
        }

        // prepara oggetto finale
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
            finalObjectOriginalScale = objectToActivate.transform.localScale;
            objectToActivate.transform.localScale = Vector3.zero;
        }

        // avvia animazione di shrink iniziale
        StartCoroutine(ShrinkAndDeactivateObjects(objectsToShrink, shrinkDuration));

        // avvia effetti principali
        effectCoroutine = StartCoroutine(RunEffectsForDuration(effectDuration));
    }

    private IEnumerator RunEffectsForDuration(float duration)
    {
        // attende che gli oggetti abbiano finito di rimpicciolirsi
        yield return new WaitForSeconds(shrinkDuration);

        effectsActive = true;
        elapsedTime = 0f;

        // avvia audio se disponibile
        if (audioSource != null)
        {
            audioSource.volume = 0f;
            audioSource.Play();
        }

        yield return new WaitForSeconds(duration);

        effectsActive = false;
        ResetAll();

        // attiva oggetto finale con effetto di scala e tremolio
        if (objectToActivate != null)
        {
            StartCoroutine(ScaleInObject(objectToActivate, scaleDuration, () =>
            {
                StartCoroutine(FinalObjectShake(objectToActivate, finalShakeDuration, finalShakeIntensity));
            }));
        }
    }

    // ===========================================================
    // RESET
    // ===========================================================
    private void ResetAll()
    {
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

        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
                kvp.Key.transform.position = kvp.Value;
        }

        foreach (var light in lights)
        {
            if (light == null) continue;
            light.enabled = false;
        }

        // ferma audio
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    // ===========================================================
    // EFFETTI
    // ===========================================================
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
        foreach (var obj in rotatingObjects)
        {
            if (obj == null) continue;
            obj.transform.Rotate(rotationAxis.normalized, 100f * speed * Time.deltaTime, Space.Self);
        }
    }

    private void HandleShake(float speed, float intensityFactor)
    {
        foreach (var obj in shakingObjects)
        {
            if (obj == null || !originalPositions.ContainsKey(obj)) continue;

            Vector3 basePos = originalPositions[obj];
            float intensity = 0.002f * intensityFactor;
            float shakeSpeed = speed * 10f;

            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * intensity;
            float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * intensity;
            obj.transform.position = basePos + new Vector3(offsetX, offsetY, 0f);
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

    private void HandleAudio(float intensity)
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Lerp(0f, maxVolume, intensity);
    }

    // ===========================================================
    // SCALING COROUTINES
    // ===========================================================
    private IEnumerator ScaleInObject(GameObject target, float duration, System.Action onComplete = null)
    {
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.transform.localScale = Vector3.Lerp(Vector3.zero, finalObjectOriginalScale, eased);
            yield return null;
        }

        target.transform.localScale = finalObjectOriginalScale;
        onComplete?.Invoke();
    }

    private IEnumerator ShrinkAndDeactivateObjects(List<GameObject> targets, float duration)
    {
        Dictionary<GameObject, Vector3> startScales = new Dictionary<GameObject, Vector3>();
        foreach (var obj in targets)
        {
            if (obj != null)
                startScales[obj] = obj.transform.localScale;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            foreach (var kvp in startScales)
            {
                if (kvp.Key != null)
                    kvp.Key.transform.localScale = Vector3.Lerp(kvp.Value, Vector3.zero, eased);
            }
            yield return null;
        }

        foreach (var kvp in startScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localScale = Vector3.zero;
                kvp.Key.SetActive(false);
            }
        }
    }

    // ===========================================================
    // TREMOLO OGGETTO FINALE
    // ===========================================================
    private IEnumerator FinalObjectShake(GameObject target, float duration, float intensity)
    {
        Vector3 originalPos = target.transform.position;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float offsetX = Mathf.Sin(timer * 10f) * intensity;
            float offsetY = Mathf.Cos(timer * 12f) * intensity;
            target.transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }
        target.transform.position = originalPos;
    }
}