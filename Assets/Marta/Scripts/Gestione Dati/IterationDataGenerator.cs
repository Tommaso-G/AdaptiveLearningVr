using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DocumentUI
{
    public TMP_Text firstText;
    public TMP_Text secondText;

    public Image firstImage;
    public Image secondImage;
}

[System.Serializable]
public class TextIconPair
{
    public TMP_Text text;
    public Sprite icon;

}

[System.Serializable]
public class IconLabelPair
{
    public Sprite icon;
    public string label;
}

public class IterationDataGenerator : MonoBehaviour
{
    [Header("Profilo apprendimento")]
    public LearningProfile learningProfile;

    [Header("Documenti (3)")]
    public DocumentUI[] documents;

    [Header("Icone primo numero (21-24)")]
    public Sprite icon21;
    public Sprite icon22;
    public Sprite icon23;
    public Sprite icon24;

    [Header("Icone secondo numero (1-3)")]
    public Sprite icon1;
    public Sprite icon2;
    public Sprite icon3;

    [Header("5 Image con icone uniche")]
    public Image[] targetImages;
    public IconLabelPair[] availableIcons;

    [Header("Label verbali (uno per ogni targetImage)")]
    public TMP_Text[] targetLabels;

    [Header("Icone Colorate-Strade")]
    public TextIconPair[] textIconPairs;

    [Header("Orario")]
    public TMP_Text timeText;

    public int[] generatedFirstNumbers = new int[3];
    public int[] generatedSecondNumbers = new int[3];
    public List<Sprite> assignedIcons = new List<Sprite>();
    public TimeSpan generatedTime;

    private int[] firstOptions = { 21, 22, 23, 24 };
    private int[] secondOptions = { 1, 2, 3 };

    private Dictionary<Sprite, TextIconPair> spriteToPair = new Dictionary<Sprite, TextIconPair>();
    public Dictionary<Sprite, TextIconPair> SpriteToPair => spriteToPair;

    public Action OnDocumentsGenerated;

    public bool isReady { get; private set; }

    private bool useVisualProfile;
    public bool IsVisualProfile => useVisualProfile;

    void Start()
    {
        InitializeProfile();
        GenerateAllData();
    }

    void InitializeProfile()
    {
        if (learningProfile == null)
        {
            Debug.LogWarning("LearningProfile non assegnato! Uso modalità visuale di default.");
            useVisualProfile = true;
            return;
        }

        var profile = learningProfile.GetProfileTuple();
        useVisualProfile = profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;

        Debug.Log("[DataGenerator] visivoVerbale vale: " + profile.visivoVerbale);
        Debug.Log("[DataGenerator] useVisualProfile vale: " + useVisualProfile);
    }

    public void GenerateAllData()
    {
        GenerateDocuments();
        GenerateUniqueIconsWithText();
        GenerateRandomTime();
    }

    void GenerateDocuments()
    {
        for (int i = 0; i < documents.Length; i++)
        {
            int firstNumber = firstOptions[UnityEngine.Random.Range(0, firstOptions.Length)];
            int secondNumber = secondOptions[UnityEngine.Random.Range(0, secondOptions.Length)];

            generatedFirstNumbers[i] = firstNumber;
            generatedSecondNumbers[i] = secondNumber;

            documents[i].firstText.text = firstNumber.ToString();
            documents[i].secondText.text = secondNumber.ToString();

            documents[i].firstImage.sprite = GetFirstIcon(firstNumber);
            documents[i].secondImage.sprite = GetSecondIcon(secondNumber);

            documents[i].firstImage.enabled = useVisualProfile;
            documents[i].secondImage.enabled = useVisualProfile;
        }

        isReady = true;
        OnDocumentsGenerated?.Invoke();

        print("[DataGenerator] chiamato evento");
    }

    public void GenerateFakeDocuments(DocumentUI[] fake)
    {
        do
        {
            for (int i = 0; i < fake.Length; i++)
            {
                int firstNumber = firstOptions[UnityEngine.Random.Range(0, firstOptions.Length)];
                int secondNumber = secondOptions[UnityEngine.Random.Range(0, secondOptions.Length)];

                fake[i].firstText.text = firstNumber.ToString();
                fake[i].secondText.text = secondNumber.ToString();

                fake[i].firstImage.sprite = GetFirstIcon(firstNumber);
                fake[i].secondImage.sprite = GetSecondIcon(secondNumber);

                fake[i].firstImage.enabled = useVisualProfile;
                fake[i].secondImage.enabled = useVisualProfile;
            }

        } while (IsSameAsReal(fake));
    }

    bool IsSameAsReal(DocumentUI[] fake)
    {
        for (int i = 0; i < fake.Length; i++)
        {
            if (fake[i].firstText.text != documents[i].firstText.text) return false;
            if (fake[i].secondText.text != documents[i].secondText.text) return false;
        }

        print("[DataGenerator] fake option uguali alle reali");

        return true;
    }

    Sprite GetFirstIcon(int value)
    {
        switch (value)
        {
            case 21: return icon21;
            case 22: return icon22;
            case 23: return icon23;
            case 24: return icon24;
        }

        return null;
    }

    Sprite GetSecondIcon(int value)
    {
        switch (value)
        {
            case 1: return icon1;
            case 2: return icon2;
            case 3: return icon3;
        }

        return null;
    }

    void GenerateUniqueIconsWithText()
    {
        assignedIcons.Clear();
        spriteToPair.Clear();

        List<IconLabelPair> tempIcons = new List<IconLabelPair>(availableIcons);

        for (int i = 0; i < targetImages.Length; i++)
        {
            int randomIconIndex = UnityEngine.Random.Range(0, tempIcons.Count);
            IconLabelPair chosen = tempIcons[randomIconIndex];

            targetImages[i].sprite = chosen.icon;
            targetImages[i].enabled = useVisualProfile;

            assignedIcons.Add(chosen.icon);
            TextIconPair pair = getTextIconPair(targetImages[i]);
            spriteToPair[chosen.icon] = pair;

            if (!useVisualProfile && pair != null && i < targetLabels.Length && targetLabels[i] != null)
                targetLabels[i].text = chosen.label;
            
            if (i < targetLabels.Length && targetLabels[i] != null)
                targetLabels[i].gameObject.SetActive(!useVisualProfile);

            print($"[DataGenerator] assegnata sprite: {chosen.icon.name} | label: {chosen.label} | immagine: {targetImages[i].name}");

            tempIcons.RemoveAt(randomIconIndex);
        }
    }

    public TextIconPair getTextIconPair(Image image)
    {
        TMP_Text text = image.gameObject.GetComponentInChildren<TMP_Text>(true);
        return textIconPairs.FirstOrDefault(pair => pair.text.text == text.text);
    }

    void GenerateRandomTime()
    {
        int hour = UnityEngine.Random.Range(8, 14);
        int minute = UnityEngine.Random.Range(0, 2) == 0 ? 0 : 30;

        generatedTime = new TimeSpan(hour, minute, 0);
        timeText.text = generatedTime.ToString(@"hh\:mm");
    }
}