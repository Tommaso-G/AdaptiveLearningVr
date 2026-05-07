using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RememberWordsImages : MonoBehaviour, ICompletableStep, ITrackableGameVisivoVerbale
{
    // ICompletableStep
    public bool IsCompleted { get; private set; } = false;

    // ITrackableGameVisivoVerbale
    public string GameID => "RememberWordsImages";
    public event System.Action<RoundData> OnRoundFinished;
    public event System.Action<string> OnGameFinished;

    [Header("Impostazioni")]
    public List<string> parole = new List<string>();
    public List<Sprite> immagini = new List<Sprite>();
    public float intervallo = 1.5f;

    [Header("Rounds")]
    public int roundsPerMode = 2;

    [Header("UI - Mostra fase")]
    public GameObject contenitoreMostra;

    [Tooltip("Contenitore con un'Image per mostrare le immagini nella fase 1")]
    public GameObject contenitoreMostraImmagine;

    [Header("UI - Pulsanti")]
    public Transform contenitorePulsanti;
    public GameObject pulsantePrefabTesto;
    public GameObject pulsantePrefabImmagine;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip suonoCorretto;
    public AudioClip suonoSbagliato;
    public AudioClip suonoRoundCompletato;

    private int currentRound = 0;
    private int totalRounds;
    private bool startWithWords;
    private bool useWords;

    private bool inRound = false;

    private List<int> ordineCorrettoIndici = new List<int>();
    private int indiceCorrente = 0;

    private TMP_Text testoMostra;
    private Image immagineMostra;

    private int errori = 0;
    private float tempoStart;

    // 🔒 BLOCCO INPUT
    private bool inputLocked = false;

    private void Start()
    {
        testoMostra = contenitoreMostra != null
            ? contenitoreMostra.GetComponentInChildren<TMP_Text>()
            : null;

        immagineMostra = contenitoreMostraImmagine != null
            ? contenitoreMostraImmagine.GetComponentInChildren<Image>()
            : null;

        if (contenitoreMostra != null) contenitoreMostra.SetActive(false);
        if (contenitoreMostraImmagine != null) contenitoreMostraImmagine.SetActive(false);

        totalRounds = roundsPerMode * 2;
        startWithWords = Random.value > 0.5f;
    }

    public void StartGame()
    {
        if (inRound) return;
        ResetSequence();
        StartCoroutine(TimerBeforeStart());
        inRound = true;
    }

    public void ResetSequence()
    {
        if (currentRound >= totalRounds) return;

        bool evenRound = (currentRound % 2 == 0);
        useWords = evenRound ? startWithWords : !startWithWords;

        indiceCorrente = 0;
        ordineCorrettoIndici.Clear();

        if (contenitoreMostra != null) contenitoreMostra.SetActive(false);
        if (contenitoreMostraImmagine != null) contenitoreMostraImmagine.SetActive(false);

        if (contenitorePulsanti != null)
            foreach (Transform child in contenitorePulsanti)
                Destroy(child.gameObject);

        errori = 0;
        tempoStart = Time.time;
        currentRound++;

        inputLocked = false; // reset sicurezza
    }

    private IEnumerator TimerBeforeStart()
    {
        if (contenitoreMostra != null) contenitoreMostra.SetActive(true);

        if (testoMostra != null)
        {
            foreach (string numero in new[] { "3", "2", "1" })
            {
                testoMostra.text = numero;
                yield return new WaitForSeconds(intervallo);
            }
            testoMostra.text = "";
        }

        if (useWords)
            StartCoroutine(MostraParole());
        else
        {
            if (contenitoreMostra != null) contenitoreMostra.SetActive(false);
            StartCoroutine(MostraImmagini());
        }
    }

    private IEnumerator MostraParole()
    {
        List<int> indici = new List<int>();
        for (int i = 0; i < parole.Count; i++) indici.Add(i);
        indici.Shuffle();

        ordineCorrettoIndici = new List<int>(indici);

        foreach (int idx in indici)
        {
            if (testoMostra != null) testoMostra.text = parole[idx];
            yield return new WaitForSeconds(intervallo);
        }

        if (contenitoreMostra != null) contenitoreMostra.SetActive(false);
        MostraPulsantiTesto();
    }

    private IEnumerator MostraImmagini()
    {
        if (contenitoreMostraImmagine != null)
            contenitoreMostraImmagine.SetActive(true);

        List<int> indici = new List<int>();
        for (int i = 0; i < immagini.Count; i++) indici.Add(i);
        indici.Shuffle();

        ordineCorrettoIndici = new List<int>(indici);

        foreach (int idx in indici)
        {
            if (immagineMostra != null)
                immagineMostra.sprite = immagini[idx];

            yield return new WaitForSeconds(intervallo);
        }

        if (contenitoreMostraImmagine != null)
            contenitoreMostraImmagine.SetActive(false);

        MostraPulsantiImmagine();
    }

    private void MostraPulsantiTesto()
    {
        List<int> ordineMisto = new List<int>();
        for (int i = 0; i < parole.Count; i++) ordineMisto.Add(i);
        ordineMisto.Shuffle();

        foreach (int idx in ordineMisto)
        {
            GameObject pulsante = Instantiate(pulsantePrefabTesto, contenitorePulsanti);

            TMP_Text testo = pulsante.GetComponentInChildren<TMP_Text>();
            if (testo != null) testo.text = parole[idx];

            Image img = pulsante.GetComponent<Image>();
            Button btn = pulsante.GetComponent<Button>();

            int capturedIdx = idx;
            btn.onClick.AddListener(() => SelezionaElemento(capturedIdx, pulsante, img));
        }
    }

    private void MostraPulsantiImmagine()
    {
        List<int> ordineMisto = new List<int>();
        for (int i = 0; i < immagini.Count; i++) ordineMisto.Add(i);
        ordineMisto.Shuffle();

        foreach (int idx in ordineMisto)
        {
            GameObject pulsante = Instantiate(pulsantePrefabImmagine, contenitorePulsanti);

            Image[] imgs = pulsante.GetComponentsInChildren<Image>();
            Image imgContenuto = imgs.Length > 1 ? imgs[1] : imgs[0];
            imgContenuto.sprite = immagini[idx];

            Image imgBackground = pulsante.GetComponent<Image>();
            Button btn = pulsante.GetComponent<Button>();

            int capturedIdx = idx;
            btn.onClick.AddListener(() => SelezionaElemento(capturedIdx, pulsante, imgBackground));
        }
    }

    private void SelezionaElemento(int indice, GameObject pulsante, Image imageBottone)
    {
        if (inputLocked) return;

        inputLocked = true;
        StartCoroutine(UnlockInputAfterDelay(1f));

        if (indiceCorrente >= ordineCorrettoIndici.Count) return;

        if (indice == ordineCorrettoIndici[indiceCorrente])
        {
            if (audioSource != null && suonoCorretto != null)
                audioSource.PlayOneShot(suonoCorretto);

            StartCoroutine(GreenThenDestroy(pulsante, imageBottone));
            indiceCorrente++;

            if (indiceCorrente == ordineCorrettoIndici.Count)
                StartCoroutine(OnRoundComplete());
        }
        else
        {
            errori++;
            if (audioSource != null && suonoSbagliato != null)
                audioSource.PlayOneShot(suonoSbagliato);

            Color prev = imageBottone != null ? imageBottone.color : Color.white;
            StartCoroutine(RedBlink(pulsante, imageBottone, prev));
        }
    }

    private IEnumerator UnlockInputAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        inputLocked = false;
    }

    private IEnumerator OnRoundComplete()
    {
        OnRoundFinished?.Invoke(new RoundData
        {
            gameID = GameID,
            modalita = useWords ? ModalitaGioco.Verbale : ModalitaGioco.Visivo,
            numeroRound = currentRound,
            errori = errori,
            tempoSecondi = Time.time - tempoStart
        });

        if (audioSource != null && suonoRoundCompletato != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(suonoRoundCompletato);
        }

        if (currentRound >= totalRounds)
        {
            IsCompleted = true;
        }
        else
        {
            inRound = false;
            yield return new WaitForSeconds(
                audioSource != null && suonoRoundCompletato != null
                    ? suonoRoundCompletato.length + 0.5f
                    : 1f);
        }
    }

    private IEnumerator GreenThenDestroy(GameObject pulsante, Image img)
    {
        if (img != null) img.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        Destroy(pulsante);
    }

    private IEnumerator RedBlink(GameObject pulsante, Image img, Color coloreOriginale)
    {
        if (img != null) img.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        if (img != null) img.color = coloreOriginale;
    }
}

public static class ListExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}