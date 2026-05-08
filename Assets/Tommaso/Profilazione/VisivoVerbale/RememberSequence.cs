using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;

[System.Serializable]
public class SequenceStep
{
    public GameObject oggetto;

    [TextArea]
    public string testo;

    public Sprite immagine;
}

public class RememberSequence : MonoBehaviour, ICompletableStep, ITrackableGameVisivoVerbale
{
    // ICompletableStep
    public bool IsCompleted { get; private set; } = false;

    // ITrackableGameVisivoVerbale
    public string GameID => "RememberSequence";
    public event System.Action<RoundData> OnRoundFinished;

    [Tooltip("Lista di oggetti da completare con testo e immagine")]
    public List<SequenceStep> sequenza = new List<SequenceStep>();

    [Header("Impostazioni layout")]
    public bool usaImmagini = true;

    public GameObject layoutImmagini;
    public GameObject layoutTesti;

    public GameObject prefabImmagine;
    public GameObject prefabTesto;

    public List<GameObject> oggettiAggiuntivi = new List<GameObject>();
    public List<string> tagDaResettare = new List<string>();

    private int indiceCorrente = 0;
    public bool hasStarted = false;

    [Header("Rounds")]
    [Tooltip("Quante volte ripetere ogni modalità")]
    public int roundsPerMode = 2;

    private int currentRound = 0;
    private int totalRounds;
    private bool startWithImages;

    private int errori = 0;
    private float tempoStart;

    private int _riapertureImmagini = 0;
    private int _riapertureTesti = 0;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip suonoStepCorretto;
    public AudioClip suonoStepSbagliato;
    public AudioClip suonoRoundCompletato;

    private readonly HashSet<GameObject> oggettiGiaAccettati = new HashSet<GameObject>();

    [Header("Gaze Tracking UI")]
    public GameObject panelImmagini;
    public GameObject panelTesti;

    [Header("Area di attivazione")]
    public Collider zonaAttivazione;
    public Transform player;

    private float tempoGuardatoImmagini = 0f;
    private float tempoGuardatoTesti = 0f;

    private float _gazeStart = 0f;
    private Coroutine _gazeCoroutine = null;
    private bool _gazeStopTimer = false;
    private bool _gazeIsImmagini = false;

    private Dictionary<GameObject, float> _ultimoErroreTime = new Dictionary<GameObject, float>();
    public float cooldownErrore = 2f;

    private class StatoIniziale
    {
        public Vector3 posizione;
        public Quaternion rotazione;

        public StatoIniziale(Vector3 pos, Quaternion rot)
        {
            posizione = pos;
            rotazione = rot;
        }
    }

    private Dictionary<GameObject, StatoIniziale> statiIniziali =
        new Dictionary<GameObject, StatoIniziale>();

    private Dictionary<GameObject, Coroutine> _lampeggiAttivi =
        new Dictionary<GameObject, Coroutine>();

    private void Start()
    {
        statiIniziali.Clear();

        foreach (string tag in tagDaResettare)
        {
            GameObject[] trovati = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject obj in trovati)
            {
                if (!statiIniziali.ContainsKey(obj))
                {
                    statiIniziali[obj] =
                        new StatoIniziale(obj.transform.position, obj.transform.rotation);
                }
            }
        }

        foreach (GameObject extra in oggettiAggiuntivi)
        {
            if (extra != null && !statiIniziali.ContainsKey(extra))
            {
                statiIniziali[extra] =
                    new StatoIniziale(extra.transform.position, extra.transform.rotation);
            }
        }

        foreach (var step in sequenza)
        {
            if (step.oggetto != null && !statiIniziali.ContainsKey(step.oggetto))
            {
                statiIniziali[step.oggetto] =
                    new StatoIniziale(
                        step.oggetto.transform.position,
                        step.oggetto.transform.rotation
                    );
            }
        }

        totalRounds = roundsPerMode * 2;
        startWithImages = Random.value > 0.5f;
        currentRound = 0;
    }

    private void Update()
    {
        if (sequenza == null || sequenza.Count == 0) return;
        if (indiceCorrente < 0 || indiceCorrente >= sequenza.Count) return;

        if (hasStarted && player != null && zonaAttivazione != null)
        {
            bool playerNellaZona = zonaAttivazione.bounds.Contains(player.position);

            if (!playerNellaZona)
            {
                if (layoutImmagini != null)
                    layoutImmagini.SetActive(false);

                if (layoutTesti != null)
                    layoutTesti.SetActive(false);
            }
        }
    }

    public void ResetSequence()
    {
        if (currentRound >= totalRounds)
            return;

        bool evenRound = (currentRound % 2 == 0);
        usaImmagini = evenRound ? startWithImages : !startWithImages;

        hasStarted = true;

        Debug.Log(
            $"Round {currentRound + 1}/{totalRounds} — Modalità: {(usaImmagini ? "Immagini" : "Testi")}"
        );

        errori = 0;
        tempoStart = Time.time;
        tempoGuardatoImmagini = 0f;
        tempoGuardatoTesti = 0f;

        if (sequenza.Count == 0)
        {
            Debug.LogWarning("Nessun oggetto nella sequenza!");
            return;
        }

        MischiaEAvvia();
        StartCoroutine(ResetConTransizione());

        currentRound++;
    }

    public void AttivaOAvviaGioco()
    {
        if (player != null && zonaAttivazione != null)
        {
            bool playerNellaZona = zonaAttivazione.bounds.Contains(player.position);

            if (!playerNellaZona)
            {
                Debug.Log("⛔ Player fuori zona.");
                return;
            }
        }

        if (!hasStarted)
        {
            ResetSequence();
            return;
        }

        if (indiceCorrente >= sequenza.Count)
        {
            if (currentRound < totalRounds)
                ResetSequence();
            else
                Debug.Log("ℹ️ Tutti i round completati.");

            return;
        }

        GameObject pannelloAttivo = usaImmagini ? layoutImmagini : layoutTesti;

        if (pannelloAttivo != null && pannelloAttivo.activeSelf)
        {
            Debug.Log("ℹ️ Pannello già attivo.");
            return;
        }

        MostraSequenzaInLayout();

        if (usaImmagini)
            _riapertureImmagini++;
        else
            _riapertureTesti++;

        Debug.Log("✅ Pannello riattivato.");
    }

    private IEnumerator ResetConTransizione()
    {
        if (layoutImmagini != null)
            layoutImmagini.SetActive(false);

        if (layoutTesti != null)
            layoutTesti.SetActive(false);

        List<Coroutine> movimenti = new List<Coroutine>();

        foreach (var kvp in statiIniziali)
        {
            if (kvp.Key != null)
            {
                movimenti.Add(
                    StartCoroutine(
                        MuoviVersoPosizioneERotazione(
                            kvp.Key,
                            kvp.Value.posizione,
                            kvp.Value.rotazione,
                            1.5f
                        )
                    )
                );
            }
        }

        foreach (var c in movimenti)
            yield return c;

        foreach (var step in sequenza)
        {
            if (step.oggetto != null)
                ResetLeva(step.oggetto);
        }

        MostraSequenzaInLayout();
    }

    private void MischiaEAvvia()
    {
        indiceCorrente = 0;

        oggettiGiaAccettati.Clear();
        _ultimoErroreTime.Clear();

        Shuffle(sequenza);
    }

    public void StepCompletato(GameObject oggetto)
    {
        if (indiceCorrente >= sequenza.Count || !hasStarted)
            return;

        if (oggettiGiaAccettati.Contains(oggetto))
            return;

        GameObject stepCorrente = sequenza[indiceCorrente].oggetto;

        if (oggetto == stepCorrente)
        {
            oggettiGiaAccettati.Add(oggetto);

            if (audioSource != null && suonoStepCorretto != null)
                audioSource.PlayOneShot(suonoStepCorretto);

            indiceCorrente++;

            if (indiceCorrente < sequenza.Count)
            {
                Debug.Log($"➡️ Prossimo step: {sequenza[indiceCorrente].oggetto.name}");
            }
            else
            {
                Debug.Log($"🏁 Round {currentRound}/{totalRounds} completato!");

                if (_gazeCoroutine != null)
                {
                    float delta = Time.time - _gazeStart;

                    if (_gazeIsImmagini)
                        tempoGuardatoImmagini += delta;
                    else
                        tempoGuardatoTesti += delta;

                    _gazeCoroutine = null;
                }

                OnRoundFinished?.Invoke(
                    new RoundData
                    {
                        gameID = GameID,
                        modalita = usaImmagini
                            ? ModalitaGioco.Visivo
                            : ModalitaGioco.Verbale,
                        numeroRound = currentRound,
                        errori = errori,
                        tempoSecondi = Time.time - tempoStart,
                        parametriExtra = usaImmagini
                            ? new Dictionary<string, float>
                            {
                                { "tempoGuardatoImmagini", tempoGuardatoImmagini },
                                { "riapertureImmagini", _riapertureImmagini }
                            }
                            : new Dictionary<string, float>
                            {
                                { "tempoGuardatoTesti", tempoGuardatoTesti },
                                { "riapertureTesti", _riapertureTesti }
                            }
                    }
                );

                if (audioSource != null && suonoRoundCompletato != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(suonoRoundCompletato);
                }

                if (layoutImmagini != null)
                    layoutImmagini.SetActive(false);

                if (layoutTesti != null)
                    layoutTesti.SetActive(false);

                if (currentRound >= totalRounds)
                {
                    Debug.Log("✅ Tutti i round completati!");
                    IsCompleted = true;
                }
            }
        }
        else
        {
            if (_ultimoErroreTime.TryGetValue(oggetto, out float ultimoTempo))
            {
                if (Time.time - ultimoTempo < cooldownErrore)
                    return;
            }

            _ultimoErroreTime[oggetto] = Time.time;

            errori++;

            Debug.Log($"❌ ERRORE: {oggetto.name}");
            Debug.Log($"🎯 STEP CORRENTE: {stepCorrente.name}");

            if (audioSource != null && suonoStepSbagliato != null)
                audioSource.PlayOneShot(suonoStepSbagliato);

            Coroutine c = StartCoroutine(LampeggiaRosso(oggetto));
            _lampeggiAttivi[oggetto] = c;

            if (statiIniziali.TryGetValue(oggetto, out StatoIniziale stato))
            {
                StartCoroutine(ResetSingoloOggetto(oggetto, stato));
            }
        }
    }

    private void MostraSequenzaInLayout()
    {
        if (layoutImmagini == null || layoutTesti == null)
        {
            Debug.LogError("❌ Layout mancanti!");
            return;
        }

        layoutImmagini.SetActive(usaImmagini);
        layoutTesti.SetActive(!usaImmagini);

        GameObject layoutAttivo = usaImmagini ? layoutImmagini : layoutTesti;
        GameObject prefabAttivo = usaImmagini ? prefabImmagine : prefabTesto;

        if (prefabAttivo == null)
        {
            Debug.LogError("❌ Prefab mancante!");
            return;
        }

        int figliAttuali = layoutAttivo.transform.childCount;
        int elementiNecessari = sequenza.Count;

        if (figliAttuali < elementiNecessari)
        {
            int daAggiungere = elementiNecessari - figliAttuali;

            for (int i = 0; i < daAggiungere; i++)
                Instantiate(prefabAttivo, layoutAttivo.transform);
        }

        for (int i = 0; i < layoutAttivo.transform.childCount; i++)
        {
            Transform child = layoutAttivo.transform.GetChild(i);

            if (i < sequenza.Count)
            {
                child.gameObject.SetActive(true);

                var step = sequenza[i];

                if (usaImmagini)
                {
                    Image img = child.GetComponent<Image>();

                    if (img != null)
                        img.sprite = step.immagine;
                }
                else
                {
                    TMP_Text tmp = child.GetComponent<TMP_Text>();

                    if (tmp != null)
                    {
                        tmp.text = step.testo;
                    }
                    else
                    {
                        Text legacy = child.GetComponent<Text>();

                        if (legacy != null)
                            legacy.text = step.testo;
                    }
                }
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void Shuffle<T>(IList<T> lista)
    {
        System.Random rng = new System.Random();

        int n = lista.Count;

        while (n > 1)
        {
            n--;

            int k = rng.Next(n + 1);

            (lista[k], lista[n]) = (lista[n], lista[k]);
        }
    }

    public IEnumerator LampeggiaRosso(
        GameObject oggetto,
        float durata = 2f,
        float frequenza = 4f
    )
    {
        if (oggetto == null)
            yield break;

        Renderer[] renderers = oggetto.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            yield break;

        List<Color> coloriOriginali = new List<Color>();
        List<Color> emissioniOriginali = new List<Color>();
        List<Material> materiali = new List<Material>();

        foreach (var r in renderers)
        {
            Material shared = r.sharedMaterial;

            coloriOriginali.Add(
                shared != null && shared.HasProperty("_Color")
                    ? shared.color
                    : Color.white
            );

            emissioniOriginali.Add(
                shared != null && shared.HasProperty("_EmissionColor")
                    ? shared.GetColor("_EmissionColor")
                    : Color.black
            );

            materiali.Add(r.material);
        }

        float tempo = 0f;
        float periodo = 1f / frequenza;

        while (tempo < durata)
        {
            bool acceso = Mathf.FloorToInt(tempo / periodo) % 2 == 0;

            for (int i = 0; i < materiali.Count; i++)
            {
                Material mat = materiali[i];

                if (mat == null)
                    continue;

                if (acceso)
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = Color.red;

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.red * 2f);
                    }
                }
                else
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = coloriOriginali[i];

                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", emissioniOriginali[i]);
                }
            }

            tempo += Time.deltaTime;

            yield return null;
        }

        for (int i = 0; i < materiali.Count; i++)
        {
            Material mat = materiali[i];

            if (mat == null)
                continue;

            if (mat.HasProperty("_Color"))
                mat.color = coloriOriginali[i];

            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissioniOriginali[i]);
        }

        _lampeggiAttivi.Remove(oggetto);
    }

    private IEnumerator MuoviVersoPosizioneERotazione(
        GameObject oggetto,
        Vector3 destinazione,
        Quaternion rotazioneDestinazione,
        float durata
    )
    {
        if (oggetto == null)
            yield break;

        Vector3 posizioneIniziale = oggetto.transform.position;
        Quaternion rotazioneIniziale = oggetto.transform.rotation;

        float tempoTrascorso = 0f;

        Collider[] colliders = oggetto.GetComponentsInChildren<Collider>();
        Rigidbody[] rigidbodies = oggetto.GetComponentsInChildren<Rigidbody>();

        Dictionary<Rigidbody, bool> statiKinematic =
            new Dictionary<Rigidbody, bool>();

        foreach (Rigidbody rb in rigidbodies)
        {
            statiKinematic[rb] = rb.isKinematic;
            rb.isKinematic = true;
        }

        foreach (Collider c in colliders)
            c.enabled = false;

        while (tempoTrascorso < durata)
        {
            tempoTrascorso += Time.deltaTime;

            float t = tempoTrascorso / durata;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            oggetto.transform.position =
                Vector3.Lerp(posizioneIniziale, destinazione, smoothT);

            oggetto.transform.rotation =
                Quaternion.Slerp(
                    rotazioneIniziale,
                    rotazioneDestinazione,
                    smoothT
                );

            yield return null;
        }

        oggetto.transform.position = destinazione;
        oggetto.transform.rotation = rotazioneDestinazione;

        foreach (Collider c in colliders)
            c.enabled = true;

        foreach (var kvp in statiKinematic)
        {
            if (kvp.Key != null)
                kvp.Key.isKinematic = kvp.Value;
        }
    }

    private IEnumerator ResetSingoloOggetto(
        GameObject oggetto,
        StatoIniziale stato
    )
    {
        if (_lampeggiAttivi.TryGetValue(oggetto, out Coroutine lampeggio)
            && lampeggio != null)
        {
            yield return lampeggio;
        }

        if (oggetto == null)
            yield break;

        var interactable =
            oggetto.GetComponentInChildren<
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
            >();

        if (interactable != null)
        {
            while (interactable.isSelected)
                yield return null;
        }

        yield return StartCoroutine(
            MuoviVersoPosizioneERotazione(
                oggetto,
                stato.posizione,
                stato.rotazione,
                1.5f
            )
        );
    }

    public void GazeSelectionImmagini() => StartGaze(true);
    public void GazeDeselectionImmagini() => StopGaze(true);

    public void GazeSelectionTesti() => StartGaze(false);
    public void GazeDeselectionTesti() => StopGaze(false);

    private void StartGaze(bool isImmagini)
    {
        if (_gazeCoroutine != null)
            return;

        if (isImmagini != usaImmagini)
            return;

        _gazeIsImmagini = isImmagini;
        _gazeStopTimer = false;

        _gazeCoroutine = StartCoroutine(GazeTimer());
    }

    private void StopGaze(bool isImmagini)
    {
        if (_gazeCoroutine == null || _gazeIsImmagini != isImmagini)
            return;

        _gazeStopTimer = true;
    }

    private IEnumerator GazeTimer()
    {
        _gazeStart = Time.time;

        while (!_gazeStopTimer)
            yield return null;

        float delta = Time.time - _gazeStart;

        if (_gazeIsImmagini)
            tempoGuardatoImmagini += delta;
        else
            tempoGuardatoTesti += delta;

        _gazeCoroutine = null;
    }

    private void ResetLeva(GameObject oggetto)
    {
        XRLever leva = oggetto.GetComponentInChildren<XRLever>();

        if (leva == null)
            return;

        leva.value = true;

        Debug.Log($"🔄 Leva resettata su {oggetto.name}");
    }
}