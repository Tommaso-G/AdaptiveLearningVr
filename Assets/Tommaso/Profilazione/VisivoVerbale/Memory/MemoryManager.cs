using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SocialPlatforms.Impl;

public class MemoryManager : MonoBehaviour
{
    [Header("Impostazioni")]
    public bool useImage = true; // Se true usa immagini, altrimenti parole
    public Transform parentObject; // Oggetto contenitore dei 16 figli

    [Header("Contenuti")]
    public List<Sprite> images = new List<Sprite>(8);
    public List<string> words = new List<string>(8);

    public float revealDuration = 3f;

    private List<GameObject> children = new List<GameObject>();

    private CardFlipper firstFlippedCard = null;
    private CardFlipper secondFlippedCard = null;

    private bool canInteract = true;

    public bool CanInteract() => canInteract;

    private int score = 0;

    public AudioSource source;   
    public AudioClip mismatchSound;   
    public AudioClip matchSound;  
    public AudioClip victorySound;

    void Start()
    {
        if (parentObject == null)
        {
            Debug.LogError("ParentObject non assegnato!");
            return;
        }

        // Recupera tutti i figli
        foreach (Transform child in parentObject)
        {
            children.Add(child.gameObject);
        }

        if (children.Count != 16)
        {
            Debug.LogWarning("Il numero di figli non è 16. Attualmente: " + children.Count);
        }

        // Disattiva il componente non usato
        foreach (var child in children)
        {
            Image img = child.GetComponentInChildren<Image>(true);
            TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>(true);

            if (img != null) img.enabled = useImage;
            if (txt != null) txt.enabled = !useImage;
            
        }

        // Assegna i contenuti
        if (useImage)
            AssignImages();
        else
            AssignWords();

        // --- ASSEGNA GLI ID ALLE CARTE ---
        for (int i = 0; i < children.Count; i++)
        {
            var flipper = children[i].GetComponent<CardFlipper>();
            if (flipper != null)
            {
                if (useImage)
                {
                    var img = children[i].GetComponentInChildren<Image>();
                    if (img != null)
                        flipper.cardID = img.sprite.name; // ID = nome sprite
                }
                else
                {
                    var txt = children[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                        flipper.cardID = txt.text; // ID = parola
                }

                flipper.memoryManager = this; // collega il MemoryManager
            }
        }
    }

    void AssignImages()
    {
        if (images.Count < 8)
        {
            Debug.LogError("Servono almeno 8 immagini!");
            return;
        }

        List<Sprite> pairedImages = new List<Sprite>();
        foreach (var img in images)
        {
            pairedImages.Add(img);
            pairedImages.Add(img);
        }

        Shuffle(pairedImages);

        for (int i = 0; i < Mathf.Min(children.Count, pairedImages.Count); i++)
        {
            Image imgComp = children[i].GetComponentInChildren<Image>();
            if (imgComp != null)
            {
                imgComp.sprite = pairedImages[i];
                imgComp.enabled = true;
            }

            TextMeshProUGUI txtComp = children[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txtComp != null)
                txtComp.text = "";
        }
    }

    void AssignWords()
    {
        if (words.Count < 8)
        {
            Debug.LogError("Servono almeno 8 parole!");
            return;
        }

        List<string> pairedWords = new List<string>();
        foreach (var w in words)
        {
            pairedWords.Add(w);
            pairedWords.Add(w);
        }

        Shuffle(pairedWords);

        for (int i = 0; i < Mathf.Min(children.Count, pairedWords.Count); i++)
        {
            TextMeshProUGUI txtComp = children[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txtComp != null)
            {
                txtComp.text = pairedWords[i];
                txtComp.enabled = true;
            }

            Image imgComp = children[i].GetComponentInChildren<Image>();
            if (imgComp != null)
                imgComp.enabled = false;
        }
    }

    // Funzione di shuffle generica
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

    public void StartGame()
    {
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
                StartCoroutine(flipper.FlipCard(-180f));
                flipper.inGame = true;
        }
 
    }

    public void OnCardSelected(CardFlipper selectedCard)
    {
        // Se non c'è ancora la prima carta, impostala
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
            if(score == children.Count/2)
                victoryRoyale();

            
        }
        else
        {
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

    private void victoryRoyale()
    {
            source.clip = victorySound;
            source.Play();
    }

    

}