using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class QuizManager : MonoBehaviour
{
    public GameObject[] questions;
    public GameObject Uipanel;
    int currentquestion;

    public UnityEvent OnEnd;

    [Header("Riferimenti")]
    public IterationDataGenerator dataGenerator;

    [Header("UI Domanda 1")]
    public TMP_Text questionText;
    public Button[] answerButtons;      // size 3
    public TMP_Text[] answerTexts;      // size 3 (testi figli dei bottoni)
    public Image[] answerImages;

    [Header("Sprite Corretta")]
    public Sprite correctSprite;

    [Header("UI Domanda 2")]
    public DocumentUI[] correctOption; // dati veri
    public DocumentUI[] fakeOption;    // dati falsi

    public Button correctOptionButton;
    public Button fakeOptionButton;

    public ErrorReporter ErrorReporter;

    void Start()
    {
        SetupQuestion1();
        SetupQuestion2();
    }

    void SetupQuestion1()
    {
        TextIconPair correctPair = dataGenerator.SpriteToPair[correctSprite];
        print($"[QuizManager] correct sprite: {correctSprite.name} con testo: {correctPair.text.text} e icona: {correctPair.icon.name}");

        List<TextIconPair> availablePairs = new List<TextIconPair>();

        int totalOptions = dataGenerator.textIconPairs.Length;

        availablePairs.Add(correctPair);

        List<TextIconPair> wrongOptions = new List<TextIconPair>();

        for (int i = 0; i < totalOptions; i++)
        {
            if (dataGenerator.textIconPairs[i] != correctPair)
            {
                wrongOptions.Add(dataGenerator.textIconPairs[i]);
                print($"[QuizManager] aggiunto indice {dataGenerator.textIconPairs[i].icon.name} alla lista di wrongOptions");
            }
        }

        for (int i = 0; i < wrongOptions.Count; i++)
        {
            int rand = Random.Range(i, wrongOptions.Count);
            TextIconPair temp = wrongOptions[i];
            wrongOptions[i] = wrongOptions[rand];
            wrongOptions[rand] = temp;
        }

        availablePairs.Add(wrongOptions[0]);
        availablePairs.Add(wrongOptions[1]);
        print($"[QuizManager] aggiunto indici {wrongOptions[0].icon.name} e {wrongOptions[1].icon.name} alla lista di opzioni");

        for (int i = 0; i < availablePairs.Count; i++)
        {
            int rand = Random.Range(i, availablePairs.Count);
            TextIconPair temp = availablePairs[i];
            availablePairs[i] = availablePairs[rand];
            availablePairs[rand] = temp;
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            TextIconPair slotPair = availablePairs[i];
            print($"[QuizManager] slotIndex attuale: {slotPair.icon.name}");

            answerTexts[i].text = slotPair.text.text;

            answerImages[i].sprite = slotPair.icon;
            answerImages[i].gameObject.SetActive(dataGenerator.IsVisualProfile); // ←

            Debug.Log($"[QuizManager] Button {i}: {(answerButtons[i] == null ? "NULL" : answerButtons[i].name)}");

            answerButtons[i].onClick.RemoveAllListeners();

            print($"[QuizManager] Slot index: {slotPair.icon.name} == Correct index: {correctPair.icon.name} ? -> {slotPair == correctPair}");
            if (slotPair == correctPair)
            {
                Button buttonCopy = answerButtons[i];
                print($"[QuizManager] assegnato correct button a {buttonCopy.name}");
                buttonCopy.onClick.AddListener(CorrectAnswer);
            }
            else
            {
                Button buttonCopy = answerButtons[i];
                print($"[QuizManager] assegnato wrong button a {buttonCopy.name}");
                buttonCopy.onClick.AddListener(WrongAnswer);
            }
        }
    }

    void SetupQuestion2()
    {
        bool correctLeft = Random.value > 0.5f;

        correctOptionButton.onClick.RemoveAllListeners();
        fakeOptionButton.onClick.RemoveAllListeners();

        if (correctLeft)
        {
            RenderOption(correctOption, dataGenerator.documents);
            dataGenerator.GenerateFakeDocuments(fakeOption);
            SetDocumentImagesActive(fakeOption, dataGenerator.IsVisualProfile); // ←

            correctOptionButton.onClick.AddListener(CorrectAnswer);
            fakeOptionButton.onClick.AddListener(WrongAnswer);
        }
        else
        {
            dataGenerator.GenerateFakeDocuments(correctOption);
            SetDocumentImagesActive(correctOption, dataGenerator.IsVisualProfile); // ←
            RenderOption(fakeOption, dataGenerator.documents);

            correctOptionButton.onClick.AddListener(WrongAnswer);
            fakeOptionButton.onClick.AddListener(CorrectAnswer);
        }
    }

    void RenderOption(DocumentUI[] targetOption, DocumentUI[] sourceDocuments)
    {
        for (int i = 0; i < targetOption.Length; i++)
        {
            targetOption[i].firstText.text = sourceDocuments[i].firstText.text;
            targetOption[i].secondText.text = sourceDocuments[i].secondText.text;

            targetOption[i].firstImage.sprite = sourceDocuments[i].firstImage.sprite;
            targetOption[i].secondImage.sprite = sourceDocuments[i].secondImage.sprite;

            targetOption[i].firstImage.gameObject.SetActive(dataGenerator.IsVisualProfile); // ←
            targetOption[i].secondImage.gameObject.SetActive(dataGenerator.IsVisualProfile); // ←
        }
    }

    void SetDocumentImagesActive(DocumentUI[] option, bool active)
    {
        for (int i = 0; i < option.Length; i++)
        {
            option[i].firstImage.gameObject.SetActive(active);
            option[i].secondImage.gameObject.SetActive(active);
        }
    }

    public void CorrectAnswer()
    {
        if (currentquestion + 1 < questions.Length)
        {
            questions[currentquestion].SetActive(false);
            StartCoroutine(NextQuestion());
        }
        else
        {
            OnEnd?.Invoke();
            Uipanel.SetActive(false);
        }
    }

    private IEnumerator NextQuestion()
    {
        yield return new WaitForSeconds(1);
        currentquestion++;
        questions[currentquestion].SetActive(true);
    }

    public void WrongAnswer()
    {
        if (ErrorReporter != null)
        {
            ErrorReporter.RegisterError(gameObject.name);
        }
        else
        {
            Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
        }
        CorrectAnswer();
    }
}