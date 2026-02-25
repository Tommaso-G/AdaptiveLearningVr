using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordMemoryGame : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Header("Impostazioni parole")]
    [Tooltip("Lista di parole da mostrare")]
    public List<string> parole = new List<string>();

    [Tooltip("Tempo tra una parola e la successiva")]
    public float intervallo = 1.5f;

    [Header("Riferimenti UI")]
    [Tooltip("Testo usato per mostrare le parole nella fase iniziale")]
    public GameObject contenitoreMostra;

    [Tooltip("Contenitore dove compariranno i pulsanti")]
    public Transform contenitorePulsanti;

    [Tooltip("Prefab di pulsante (deve contenere un TMP_Text)")]
    public GameObject pulsantePrefab;

    private List<string> ordineCorretto = new List<string>();
    private List<string> ordineUtente = new List<string>();
    private int indiceCorrente = 0;
    private int indexToComplete = 0;

    private TMP_Text testoMostra;

    private void Start()
    {
        // Recupera TMP_Text
        if (testoMostra == null)
            testoMostra = contenitoreMostra.GetComponentInChildren<TMP_Text>();

        if (testoMostra == null)
        {
            Debug.LogWarning("❌ testoMostra non trovato nel contenitore!");
            return;
        }

        if (parole.Count == 0)
        {
            Debug.LogWarning("⚠️ Nessuna parola impostata!");
            return;
        }

        contenitoreMostra.SetActive(false);
    }

    public void ResetSequence()
    {
        if (indexToComplete == 1) return;

        indiceCorrente = 0;
        ordineCorretto.Clear();
        ordineUtente.Clear();

        StartCoroutine(TimerBeforeStart());
    }

    private IEnumerator TimerBeforeStart()
    {
        contenitoreMostra.SetActive(true);
        List<string> timer = new List<string> { "3", "2", "1" };

        foreach (string numero in timer)
        {
            testoMostra.text = numero;
            yield return new WaitForSeconds(intervallo);
        }

        testoMostra.text = "";
        StartCoroutine(MostraParoleInOrdineCasuale());
    }

    private IEnumerator MostraParoleInOrdineCasuale()
    {
        // Crea una copia casuale
        List<string> ordineCasuale = new List<string>(parole);
        ordineCasuale.Shuffle();

        // Memorizza l'ordine corretto
        ordineCorretto = new List<string>(ordineCasuale);

        // Mostra le parole una alla volta
        foreach (string parola in ordineCasuale)
        {
            testoMostra.text = parola;
            yield return new WaitForSeconds(intervallo);
        }

        // Fine fase 1
        contenitoreMostra.SetActive(false);
        testoMostra.text = "";
        MostraPulsanti();
    }

    private void MostraPulsanti()
    {
        if (contenitorePulsanti == null)
        {
            Debug.LogError("❌ contenitorePulsanti è NULL");
            return;
        }

        // Rimuove pulsanti precedenti
        foreach (Transform child in contenitorePulsanti)
            Destroy(child.gameObject);

        // Crea pulsanti in ordine casuale
        List<string> ordineMisto = new List<string>(parole);
        if (ordineMisto.Count == 0) return;
        ordineMisto.Shuffle();

        foreach (string parola in ordineMisto)
        {
            if (pulsantePrefab == null)
            {
                Debug.LogError("❌ pulsantePrefab è NULL!");
                return;
            }

            GameObject pulsante = Instantiate(pulsantePrefab, contenitorePulsanti);
            pulsante.SetActive(true);

            TMP_Text testo = pulsante.GetComponentInChildren<TMP_Text>();
            if (testo == null)
            {
                Debug.LogError($"❌ Nessun TMP_Text nel prefab {pulsante.name}");
                continue;
            }
            testo.text = parola;

            Image imageBottone = pulsante.GetComponent<Image>();
            if (imageBottone == null)
            {
                Debug.LogError($"❌ Nessun componente Image nel pulsante {pulsante.name}");
                continue;
            }

            Button btn = pulsante.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"❌ Nessun componente Button nel prefab {pulsante.name}");
                continue;
            }

            string parolaCliccata = parola;
            btn.onClick.AddListener(() =>
            {
                SelezionaParola(parolaCliccata, pulsante, imageBottone);
            });
        }

        Debug.Log($"📋 Totale pulsanti generati: {contenitorePulsanti.childCount}");
    }

    private void SelezionaParola(string parola, GameObject pulsante, Image imageBottone)
    {
        if (indiceCorrente >= ordineCorretto.Count) return;

        Color previousColor = imageBottone.color;

        if (parola == ordineCorretto[indiceCorrente])
        {
            StartCoroutine(GreenThenDestroy(pulsante, imageBottone));
            ordineUtente.Add(parola);
            indiceCorrente++;

            if (indiceCorrente == ordineCorretto.Count)
            {
                indexToComplete++;
                contenitoreMostra.SetActive(true);

                if (indexToComplete == 1)
                {
                    Debug.Log("🎯 Gioco completato!");
                    IsCompleted = true;
                }
            }
        }
        else
        {
            StartCoroutine(RedBlink(pulsante, imageBottone, previousColor));
        }
    }

    private IEnumerator GreenThenDestroy(GameObject pulsante, Image imageBottone)
    {
        imageBottone.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        Destroy(pulsante);
    }

    private IEnumerator RedBlink(GameObject pulsante, Image imageBottone, Color previousColor)
    {
        imageBottone.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        imageBottone.color = previousColor;
    }
}



