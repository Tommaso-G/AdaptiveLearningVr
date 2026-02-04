using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageMemoryGame : MonoBehaviour
{
    [Header("Impostazioni immagini")]
    [Tooltip("Lista di immagini da mostrare")]
    public List<Sprite> immagini = new List<Sprite>();

    [Tooltip("Tempo tra un'immagine e la successiva")]
    public float intervallo = 1.5f;

    [Header("Riferimenti UI")]
    [Tooltip("Contenitore che mostra l'immagine nella fase iniziale")]
    public GameObject contenitoreMostra;

    [Tooltip("Contenitore dove compariranno i pulsanti delle immagini")]
    public Transform contenitorePulsanti;

    [Tooltip("Prefab del pulsante (deve contenere un componente Image)")]
    public GameObject pulsantePrefab;

    private List<Sprite> ordineCorretto = new List<Sprite>();
    private List<Sprite> ordineUtente = new List<Sprite>();
    private int indiceCorrente = 0;

    private Image immagineMostra;

    private void Start()
    {
        // Recupera l'Image che mostra le immagini iniziali
        immagineMostra = contenitoreMostra.GetComponentInChildren<Image>();

        if (immagineMostra == null)
        {
            Debug.LogWarning("immagineMostra == null");
            return;
        }
        if (immagini.Count == 0)
        {
            Debug.LogWarning("Nessuna immagine impostata!");
            return;
        }

        immagineMostra.gameObject.SetActive(false);
        StartCoroutine(TimerBeforeStart());
    }

    private IEnumerator TimerBeforeStart()
    {
        List<string> timer = new List<string> { "3", "2", "1" };
        TMP_Text testo = contenitoreMostra.GetComponentInChildren<TMP_Text>();

        foreach (string numero in timer)
        {
            if (testo != null)
                testo.text = numero;

            yield return new WaitForSeconds(intervallo);
        }

        if (testo != null)
            testo.text = "";

        StartCoroutine(MostraImmaginiInOrdineCasuale());
    }

    private IEnumerator MostraImmaginiInOrdineCasuale()
    {
        immagineMostra.gameObject.SetActive(true);
        // Crea una copia casuale delle immagini
        List<Sprite> ordineCasuale = new List<Sprite>(immagini);
        ordineCasuale.Shuffle();

        // Memorizza l'ordine corretto
        ordineCorretto = new List<Sprite>(ordineCasuale);

        // Mostra le immagini una alla volta
        foreach (Sprite img in ordineCasuale)
        {
            immagineMostra.sprite = img;
            yield return new WaitForSeconds(intervallo);
        }

        // Fine fase 1
        contenitoreMostra.SetActive(false);
        MostraPulsanti();
    }

private void MostraPulsanti()
{
    foreach (Transform child in contenitorePulsanti)
        Destroy(child.gameObject);

    // Mostra i pulsanti in ordine casuale
    List<Sprite> ordineMisto = new List<Sprite>(immagini);
    ordineMisto.Shuffle();

    foreach (Sprite img in ordineMisto)
    {
        GameObject pulsante = Instantiate(pulsantePrefab, contenitorePulsanti);
        pulsante.SetActive(true);

        // 🔹 Campo Image del figlio (mostra lo sprite)
        Image imageFiglio = pulsante.transform.Find("Image").GetComponent<Image>();
        imageFiglio.sprite = img;

        // 🔹 Campo Image del bottone (sfondo del pulsante)
        Image imageBottone = pulsante.GetComponent<Image>();

        Button btn = pulsante.GetComponent<Button>();
        Sprite immagineCliccata = img;
        btn.onClick.AddListener(() => SelezionaImmagine(immagineCliccata, pulsante, imageBottone));
    }
}


private void SelezionaImmagine(Sprite immagine, GameObject pulsante, Image imageBottone)
{
    if (indiceCorrente >= ordineCorretto.Count) return;

    // ✅ Se è corretta
    if (immagine == ordineCorretto[indiceCorrente])
    {
        StartCoroutine(GreenThenDestroy(pulsante, imageBottone));
        ordineUtente.Add(immagine);
        indiceCorrente++;

        if (indiceCorrente == ordineCorretto.Count)
        {
            Debug.Log("✅ Sequenza completata correttamente!");
        }
    }
    else
    {
        imageBottone.color = Color.red;
        Debug.Log("❌ Immagine errata!");
    }
}


private IEnumerator GreenThenDestroy(GameObject pulsante, Image imageBottone)
{
    imageBottone.color = Color.green;
    yield return new WaitForSeconds(0.5f);
    Destroy(pulsante);
}

}


// Estensione per mischiare liste
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
