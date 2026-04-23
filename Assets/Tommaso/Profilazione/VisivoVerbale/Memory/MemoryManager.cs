using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MemoryManager : MonoBehaviour, ICompletableStep, ITrackableGameVisivoVerbale
{
    public bool IsCompleted { get; private set; } = false;

    // ITrackableGameVisivoVerbale
    public string GameID => "Memory";
    public event System.Action<RoundData> OnRoundFinished;

    [Header("Impostazioni")]
    public Transform parentObject;
    public float revealDuration = 3f;

    [Header("Rounds")]
    [Tooltip("Quante volte ripetere ogni modalità (immagini E parole)")]
    public int roundsPerMode = 2;

    [Header("Contenuti")]
    public List<Sprite> images = new List<Sprite>(8);
    public List<string> words = new List<string>(8);

    [Header("Audio")]
    public AudioSource source;
    public AudioClip mismatchSound;
    public AudioClip matchSound;
    public AudioClip victorySound;

    // Stato interno
    private List<GameObject> children = new List<GameObject>();
    private CardFlipper firstFlippedCard = null;
    private CardFlipper secondFlippedCard = null;
    private bool canInteract = true;
    private int score = 0;

    // Gestione round
    private bool useImage;          // modalità corrente
    private int currentRound = 0;   // round corrente (0-based, conta tutte le "partite")
    private int totalRounds;        // roundsPerMode * 2
    private bool startWithImage;    // ordine casuale iniziale

    public bool CanInteract() => canInteract;

    private bool gameStarted = false;

    // campi per il tracking
    private int errori = 0;
    private float tempoStart;


    void Start()
    {
        if (parentObject == null)
        {
            Debug.LogError("ParentObject non assegnato!");
            return;
        }

        foreach (Transform child in parentObject)
            children.Add(child.gameObject);

        if (children.Count != 16)
            Debug.LogWarning("Il numero di figli non è 16. Attualmente: " + children.Count);

        totalRounds = roundsPerMode * 2;

        // Ordine casuale: inizia con immagini o parole
        startWithImage = Random.value > 0.5f;
        currentRound = 0;

        SetupRound();
    }

    // Configura la modalità e le carte per il round corrente
    void SetupRound()
    {
        score = 0;

        bool evenRound = (currentRound % 2 == 0);
        useImage = evenRound ? startWithImage : !startWithImage;

        Debug.Log($"Round {currentRound + 1}/{totalRounds} — Modalità: {(useImage ? "Immagini" : "Parole")}");

        StartCoroutine(ResetAndSetup());
    }

    private IEnumerator ResetAndSetup()
    {
        // Disabilita l'interazione durante il reset
        canInteract = false;
        
        // Resetta tutte le carte con animazione in parallelo
        List<Coroutine> resets = new List<Coroutine>();
        foreach (var child in children)
        {
            var flipper = child.GetComponent<CardFlipper>();
            if (flipper != null)
            {
                flipper.inGame = false;
                resets.Add(StartCoroutine(flipper.ResetCardAnimated()));
            }
        }

        // Aspetta che tutte le animazioni finiscano
        foreach (var c in resets)
            yield return c;

        // Ora aggiorna i componenti UI
        foreach (var child in children)
        {
            Image img = child.GetComponentInChildren<Image>(true);
            TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (img != null) img.enabled = useImage;
            if (txt != null) txt.enabled = !useImage;
        }

        // Assegna contenuti rimescolati
        if (useImage) AssignImages();
        else AssignWords();

        // Assegna ID e collega il manager
        for (int i = 0; i < children.Count; i++)
        {
            var flipper = children[i].GetComponent<CardFlipper>();
            if (flipper == null) continue;

            if (useImage)
            {
                var img = children[i].GetComponentInChildren<Image>();
                if (img != null) flipper.cardID = img.sprite.name;
            }
            else
            {
                var txt = children[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) flipper.cardID = txt.text;
            }

            flipper.memoryManager = this;
        }

        canInteract = true;
        errori = 0;
        gameStarted = false;
    }

    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        StartCoroutine(StartGameRoutine());
    }

    public IEnumerator StartGameRoutine()
    {
        foreach (var child in children)
        {
            var flipper = child.GetComponent<CardFlipper>();
            if (flipper != null)
                StartCoroutine(flipper.FlipCard(180f));
        }

        yield return new WaitForSeconds(revealDuration);

        foreach (var child in children)
        {
            var flipper = child.GetComponent<CardFlipper>();
            if (flipper != null)
            {
                StartCoroutine(flipper.FlipCard(-180f));
                flipper.inGame = true;
            }
        }

        tempoStart = Time.time;

    }
    void AssignImages()
    {
        if (images.Count < 8) { Debug.LogError("Servono almeno 8 immagini!"); return; }

        List<Sprite> paired = new List<Sprite>();
        foreach (var img in images) { paired.Add(img); paired.Add(img); }
        Shuffle(paired);

        for (int i = 0; i < Mathf.Min(children.Count, paired.Count); i++)
        {
            Image imgComp = children[i].GetComponentInChildren<Image>();
            if (imgComp != null) { imgComp.sprite = paired[i]; imgComp.enabled = true; }

            TextMeshProUGUI txtComp = children[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txtComp != null) txtComp.text = "";
        }
    }

    void AssignWords()
    {
        if (words.Count < 8) { Debug.LogError("Servono almeno 8 parole!"); return; }

        List<string> paired = new List<string>();
        foreach (var w in words) { paired.Add(w); paired.Add(w); }
        Shuffle(paired);

        for (int i = 0; i < Mathf.Min(children.Count, paired.Count); i++)
        {
            TextMeshProUGUI txtComp = children[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txtComp != null) { txtComp.text = paired[i]; txtComp.enabled = true; }

            Image imgComp = children[i].GetComponentInChildren<Image>();
            if (imgComp != null) imgComp.enabled = false;
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    public void OnCardSelected(CardFlipper selectedCard)
    {
        if (firstFlippedCard == null)
        {
            firstFlippedCard = selectedCard;
        }
        else if (secondFlippedCard == null && selectedCard != firstFlippedCard)
        {
            secondFlippedCard = selectedCard;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        canInteract = false;
        yield return new WaitForSeconds(0.5f);

        if (firstFlippedCard.cardID == secondFlippedCard.cardID)
        {
            firstFlippedCard = null;
            secondFlippedCard = null;

            source.clip = matchSound;
            source.Play();

            score++;
            if (score == children.Count / 2)
                StartCoroutine(OnRoundComplete());
        }
        else
        {
            errori++; 

            source.clip = mismatchSound;
            source.Play();

            StartCoroutine(firstFlippedCard.FlipCard(180f));
            StartCoroutine(secondFlippedCard.FlipCard(180f));

            yield return new WaitForSeconds(firstFlippedCard.flipDuration);

            firstFlippedCard = null;
            secondFlippedCard = null;
        }

        canInteract = true;
    }

    private IEnumerator OnRoundComplete()
    {
        currentRound++;

        OnRoundFinished?.Invoke(new RoundData
        {
            gameID = GameID,
            modalita = useImage ? ModalitaGioco.Visivo : ModalitaGioco.Verbale,
            numeroRound = currentRound,
            errori = errori,
            tempoSecondi = Time.time - tempoStart
        });

        if (currentRound >= totalRounds)
        {
            // Tutti i round completati
            source.clip = victorySound;
            source.Play();
            Debug.Log("Tutti i round completati!");
            IsCompleted = true;
        }
        else
        {
            // Suona la vittoria del round, poi passa al successivo
            source.clip = victorySound;
            source.Play();

            yield return new WaitForSeconds(2f);

            SetupRound();
        }
    }
}