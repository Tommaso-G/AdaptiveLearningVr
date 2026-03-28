using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;

[System.Serializable]
public class SequenceStep
{
    public GameObject oggetto;  // Riferimento attuale
    [HideInInspector] public string nomeOriginale; // nome dell'oggetto, salvato per sicurezza
    [TextArea] public string testo;
    public Sprite immagine;
}


public class RememberSequence : MonoBehaviour, ICompletableStep, ITrackableGameVisivoVerbale
{
    //ICompletableStep
    public bool IsCompleted { get; private set; } = false; 

    //ITrackableGameVisivoVerbale
    public string GameID => "RememberSequence"; 
    public event System.Action<RoundData> OnRoundFinished;

    [Tooltip("Lista di oggetti da completare con testo e immagine (verrà mescolata casualmente all’avvio)")]
    public List<SequenceStep> sequenza = new List<SequenceStep>();

    [Header("Impostazioni layout")]
    [Tooltip("Se true usa le immagini, altrimenti i testi")]
    public bool usaImmagini = true;

    [Tooltip("Layout contenente le immagini (deve chiamarsi 'Images')")]
    public GameObject layoutImmagini;

    [Tooltip("Layout contenente i testi (deve chiamarsi 'Texts')")]
    public GameObject layoutTesti;

    [Tooltip("Prefab dell’immagine da istanziare (deve contenere un componente Image)")]
    public GameObject prefabImmagine;

    [Tooltip("Prefab del testo da istanziare (deve contenere un componente Text o TMP_Text)")]
    public GameObject prefabTesto;
    private GameObject ultimoOggettoControllato;
    public List<GameObject> oggettiAggiuntivi = new List<GameObject>();

    public List<string> tagDaResettare = new List<string> { };

    private int indiceCorrente = 0;

    public bool hasStarted = false;

    private int indexToComplete=0;

    [Header("Rounds")]
    [Tooltip("Quante volte ripetere ogni modalità (immagini E testi)")]
    public int roundsPerMode = 2;

    private int currentRound = 0;
    private int totalRounds;
    private bool startWithImages;

    //campi per i risultati 
    private int errori = 0;
    private float tempoStart;


    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip suonoStepCorretto;
    public AudioClip suonoStepSbagliato;
    public AudioClip suonoRoundCompletato;

    /// <summary>Oggetti che hanno già completato il loro step — non possono più generare errore.</summary>
    private readonly HashSet<GameObject> oggettiGiaAccettati = new HashSet<GameObject>();

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

    private Dictionary<GameObject, StatoIniziale> statiIniziali = new Dictionary<GameObject, StatoIniziale>();

    private void Start()
    {
        if (sequenza.Count == 0)
        {
            //Debug.LogWarning("Nessun oggetto assegnato alla sequenza!");
        }

        statiIniziali.Clear();

        foreach (string tag in tagDaResettare)
        {
            GameObject[] trovati = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in trovati)
            {
                if (!statiIniziali.ContainsKey(obj))
                {
                    statiIniziali[obj] = new StatoIniziale(obj.transform.position, obj.transform.rotation);
                }
            }
        }


        foreach (GameObject extra in oggettiAggiuntivi)
        {
            if (extra != null && !statiIniziali.ContainsKey(extra))
            {
                statiIniziali[extra] = new StatoIniziale(extra.transform.position, extra.transform.rotation);
            }
        }

        foreach (var step in sequenza)
        {
        if (step.oggetto != null)
            step.nomeOriginale = step.oggetto.name;
        }

        totalRounds = roundsPerMode * 2;
        startWithImages = Random.value > 0.5f;
        currentRound = 0;
    }



    private void Update()
    {
        if (sequenza == null || sequenza.Count == 0) return;
        if (indiceCorrente < 0 || indiceCorrente >= sequenza.Count) return;

        GameObject corrente = sequenza[indiceCorrente].oggetto;
        if (corrente != ultimoOggettoControllato)
        {
            ultimoOggettoControllato = corrente;

            if (OggettoCorrenteHaComponente<Breakable>())
            {
                Breakable breakable = corrente.GetComponent<Breakable>();
                breakable.hisTurn = true;
                
            }

        }
    }

    public bool OggettoCorrenteHaComponente<T>() where T : Component
    {
    if (sequenza == null || sequenza.Count == 0)
        return false;

    if (indiceCorrente < 0 || indiceCorrente >= sequenza.Count)
        return false;

    GameObject oggettoCorrente = sequenza[indiceCorrente].oggetto;
    if (oggettoCorrente == null)
        return false;

    return oggettoCorrente.GetComponent<T>() != null;
    }

    public void ResetSequence()
    {
        if (currentRound >= totalRounds) return;

        bool evenRound = (currentRound % 2 == 0);
        usaImmagini = evenRound ? startWithImages : !startWithImages;

        Debug.Log($"Round {currentRound + 1}/{totalRounds} — Modalità: {(usaImmagini ? "Immagini" : "Testi")}");

        errori = 0;
        tempoStart = Time.time;

        RimpiazzaOggettiDistrutti();
        hasStarted = true;

        if (sequenza.Count == 0)
        {
            Debug.LogWarning("Nessun oggetto nella sequenza da resettare!");
            return;
        }

        MischiaEAvvia();
        StartCoroutine(ResetConTransizione());

        currentRound++;
    }

    private void RicollegaEventiBreakable()
    {
        foreach (var step in sequenza)
        {
            if (step.oggetto == null) continue;

            Breakable breakable = step.oggetto.GetComponent<Breakable>();
            if (breakable == null) continue;

            // Rimuovi listener vecchi per evitare duplicati
            breakable.onBreak.RemoveAllListeners();

            // Ricollega: quando viene rotto, chiama StepCompletato con l'oggetto corrente
            GameObject oggettoStep = step.oggetto;
            breakable.onBreak.AddListener((collider, brokenVersion) =>
            {
                // Rinomina subito il clone con il nome originale
                brokenVersion.name = step.nomeOriginale;
                StepCompletato(oggettoStep);
            });
        }
    }

    private IEnumerator ResetConTransizione()
    {
        if (layoutImmagini != null) layoutImmagini.SetActive(false);
        if (layoutTesti != null) layoutTesti.SetActive(false);

        List<Coroutine> movimenti = new List<Coroutine>();
        foreach (var kvp in statiIniziali)
        {
            if (kvp.Key != null)
                movimenti.Add(StartCoroutine(MuoviVersoPosizioneERotazione(
                    kvp.Key, kvp.Value.posizione, kvp.Value.rotazione, 1.5f)));
        }

        foreach (var c in movimenti)
            yield return c;

        RicollegaEventiBreakable(); // ← ricollega dopo che gli oggetti sono tornati a posto
        MostraSequenzaInLayout();
    }

    private void MischiaEAvvia()
    {
        indiceCorrente = 0;
        ultimoOggettoControllato = null;
        oggettiGiaAccettati.Clear(); // ← reset a ogni nuovo round
        Shuffle(sequenza);
    }
    public void StepCompletato(GameObject oggetto)
    {
        if (indiceCorrente >= sequenza.Count || !hasStarted)
            return;

        // Se l'oggetto ha già completato il suo step, ignora qualsiasi segnale successivo
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
                Debug.Log($"Prossimo step: {sequenza[indiceCorrente].oggetto.name}");
            }
            else
            {
                Debug.Log($"Round {currentRound}/{totalRounds} completato!");

                OnRoundFinished?.Invoke(new RoundData
                {
                    gameID = GameID,
                    modalita = usaImmagini ? ModalitaGioco.Visivo : ModalitaGioco.Verbale,
                    numeroRound = currentRound,
                    errori = errori,
                    tempoSecondi = Time.time - tempoStart
                });

                if (audioSource != null && suonoRoundCompletato != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(suonoRoundCompletato);
                }

                if (layoutImmagini != null) layoutImmagini.SetActive(false);
                if (layoutTesti != null) layoutTesti.SetActive(false);

                if (currentRound >= totalRounds)
                {
                    Debug.Log("Tutti i round completati!");
                    IsCompleted = true;
                }
            }
        }
        else
        {
            errori++;

            if (audioSource != null && suonoStepSbagliato != null)
                audioSource.PlayOneShot(suonoStepSbagliato);

            StartCoroutine(LampeggiaRosso(oggetto));
        }
    }
    
    private void MostraSequenzaInLayout()
    {
        if (layoutImmagini == null || layoutTesti == null)
        {
            Debug.LogError("❌ Layout 'Images' o 'Texts' non assegnati!");
            return;
        }

        // Attiva il layout scelto
        layoutImmagini.SetActive(usaImmagini);
        layoutTesti.SetActive(!usaImmagini);

        GameObject layoutAttivo = usaImmagini ? layoutImmagini : layoutTesti;
        GameObject prefabAttivo = usaImmagini ? prefabImmagine : prefabTesto;

        if (prefabAttivo == null)
        {
            Debug.LogError("❌ Prefab non assegnato!");
            return;
        }

        int figliAttuali = layoutAttivo.transform.childCount;
        int elementiNecessari = sequenza.Count;

        // 🔹 Se servono più elementi, ne istanzia altri
        if (figliAttuali < elementiNecessari)
        {
            int daAggiungere = elementiNecessari - figliAttuali;
            for (int i = 0; i < daAggiungere; i++)
            {
                Instantiate(prefabAttivo, layoutAttivo.transform);
            }

        }

        // 🔹 Aggiorna i contenuti e gestisce la visibilità
        for (int i = 0; i < layoutAttivo.transform.childCount; i++)
        {
            Transform child = layoutAttivo.transform.GetChild(i);

            if (i < sequenza.Count)
            {
                // Attiva e aggiorna contenuto
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
                        // supporto a UI.Text classico
                        Text legacy = child.GetComponent<Text>();
                        if (legacy != null)
                            legacy.text = step.testo;
                    }
                }
            }
            else
            {
                // Troppi elementi -> li nascondes
                child.gameObject.SetActive(false);
            }
        }

        //Debug.Log($"📋 Layout aggiornato: {(usaImmagini ? "Immagini" : "Testi")} ({sequenza.Count} elementi attivi)");
    }



    private void RimpiazzaOggettiDistrutti()
    {
        var tutti = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < sequenza.Count; i++)
        {
            var step = sequenza[i];
            if (step == null || step.oggetto != null)
                continue;

            string nomeAtteso = step.nomeOriginale;
            if (string.IsNullOrEmpty(nomeAtteso))
                continue;

            GameObject sostituto = tutti.FirstOrDefault(go =>
                go.name.Contains(nomeAtteso) && go.scene.IsValid());

            if (sostituto != null)
            {
                // Rinomina il sostituto con il nome originale pulito
                sostituto.name = nomeAtteso;
                step.oggetto = sostituto;
            }
            else
            {
                Debug.LogWarning($"❌ [Step {i}] Nessun oggetto trovato con nome contenente '{nomeAtteso}'.");
            }
        }

        sequenza.RemoveAll(s => s == null || s.oggetto == null);
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

    public IEnumerator LampeggiaRosso(GameObject oggetto, float durata = 2f, float frequenza = 4f)
    {
        if (oggetto == null)
        {
            Debug.LogWarning("⚠️ Nessun oggetto da illuminare!");
            yield break;
        }

        // Prendi tutti i renderer nell'oggetto e nei figli
        Renderer[] renderers = oggetto.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"⚠️ L'oggetto {oggetto.name} e i suoi figli non hanno Renderer!");
            yield break;
        }

        // Salva i colori originali di ogni materiale
        List<Color> coloriOriginali = new List<Color>();
        List<Color> emissioniOriginali = new List<Color>();
        List<Material> materiali = new List<Material>();

        foreach (var r in renderers)
        {
            // Usare renderer.material per creare un'istanza unica del materiale
            Material mat = r.material;
            materiali.Add(mat);

            coloriOriginali.Add(mat.HasProperty("_Color") ? mat.color : Color.white);
            emissioniOriginali.Add(mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black);
        }

        float tempo = 0f;
        float periodo = 1f / frequenza;

        while (tempo < durata)
        {
            bool acceso = Mathf.FloorToInt(tempo / periodo) % 2 == 0;

            for (int i = 0; i < materiali.Count; i++)
            {
                Material mat = materiali[i];

                if (mat == null) continue; // sicurezza in caso di distruzione

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

        // Ripristina tutti i materiali
        for (int i = 0; i < materiali.Count; i++)
        {
            Material mat = materiali[i];
            if (mat == null) continue;

            if (mat.HasProperty("_Color"))
                mat.color = coloriOriginali[i];

            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissioniOriginali[i]);
        }

        //Debug.Log($"💡 Lampeggio rosso terminato per {oggetto.name} e i suoi figli");
    }

    private IEnumerator MuoviVersoPosizioneERotazione(GameObject oggetto, Vector3 destinazione, Quaternion rotazioneDestinazione, float durata)
    {
        if (oggetto == null) yield break;

        Vector3 posizioneIniziale = oggetto.transform.position;
        Quaternion rotazioneIniziale = oggetto.transform.rotation;
        float tempoTrascorso = 0f;

        
        Collider[] colliders = oggetto.GetComponentsInChildren<Collider>();
        Rigidbody[] rigidbodies = oggetto.GetComponentsInChildren<Rigidbody>();

        
        Dictionary<Rigidbody, bool> statiKinematic = new Dictionary<Rigidbody, bool>();
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

            oggetto.transform.position = Vector3.Lerp(posizioneIniziale, destinazione, smoothT);
            oggetto.transform.rotation = Quaternion.Slerp(rotazioneIniziale, rotazioneDestinazione, smoothT);

            yield return null;
        }

        // 🔹 Imposta la posizione e la rotazione finale precise
        oggetto.transform.position = destinazione;
        oggetto.transform.rotation = rotazioneDestinazione;

        // 🔹 Riattiva collider e ripristina isKinematic originale
        foreach (Collider c in colliders)
            c.enabled = true;

        foreach (var kvp in statiKinematic)
            if (kvp.Key != null)
                kvp.Key.isKinematic = kvp.Value;
    }

}
