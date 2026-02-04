using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordMemoryGame : MonoBehaviour
{
    [Header("Impostazioni parole")]
    [Tooltip("Lista di parole da mostrare")]
    public List<string> parole = new List<string>();

    [Tooltip("Tempo tra una parola e la successiva")]
    public float intervallo = 1.5f;

    [Header("Riferimenti UI")]
    [Tooltip("Testo usato per mostrare le parole nella fase iniziale")]
    public GameObject ContenitoreMostra;

    [Tooltip("Contenitore dove compariranno i pulsanti")]
    public Transform contenitorePulsanti;

    [Tooltip("Prefab di pulsante (deve contenere un TMP_Text)")]
    public GameObject pulsantePrefab;

    private List<string> ordineCorretto = new List<string>();
    private List<string> ordineUtente = new List<string>();
    private int indiceCorrente = 0;

    private TMP_Text testoMostra;

    private void Start()
    {
        if (testoMostra == null)
            testoMostra = ContenitoreMostra.GetComponentInChildren<TMP_Text>();

        if (parole.Count == 0)
        {
            Debug.LogWarning("Nessuna parola impostata!");
            return;
        }

        StartCoroutine(TimerBeforeStart());
        
        
    }

    private IEnumerator TimerBeforeStart()
    {
        List<string> timer = new List<string>{"3","2","1"};

        foreach(string numero in timer)
        {
            testoMostra.text = numero;
            yield return new WaitForSeconds(intervallo);
        }

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
        ContenitoreMostra.SetActive(false);
        MostraPulsanti();
    }

    private void MostraPulsanti()
    {
        foreach (Transform child in contenitorePulsanti)
            Destroy(child.gameObject);

        // Mostra i pulsanti in ordine casuale (diverso dall’ordine mostrato)
        List<string> ordineMisto = new List<string>(parole);
        ordineMisto.Shuffle();

        foreach (string parola in ordineMisto)
        {
            GameObject pulsante = Instantiate(pulsantePrefab, contenitorePulsanti);
            TMP_Text testo = pulsante.GetComponentInChildren<TMP_Text>();
            testo.text = parola;

            Button btn = pulsante.GetComponent<Button>();
            string parolaCliccata = parola;
            btn.onClick.AddListener(() => SelezionaParola(parolaCliccata, pulsante));
        }
    }

    private void SelezionaParola(string parola, GameObject pulsante)
    {
        if (indiceCorrente >= ordineCorretto.Count) return;

        // Controlla se è la parola corretta
        if (parola == ordineCorretto[indiceCorrente])
        {
            StartCoroutine(GreenThenDestroy(pulsante));
            ordineUtente.Add(parola);
            indiceCorrente++;

            if (indiceCorrente == ordineCorretto.Count)
            {
                Debug.Log("✅ Sequenza completata correttamente!");
            }
        }
        else
        {
            pulsante.GetComponent<Image>().color = Color.red;
            Debug.Log("❌ Parola errata!");
        }
    }

    private IEnumerator GreenThenDestroy(GameObject pulsante)
    {
        pulsante.GetComponentInChildren<Image>().color = Color.green;
        yield return new WaitForSeconds(0.5f);
        Destroy(pulsante);
    }
}



