using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SlideDataContainer
{
    public string pageName;
    public float focusTime;
    public float normalizedFocusTime;
    public int opening;
    public LearningEnums.SequenzialeGlobale seqGlob;
    public LearningEnums.VisivoVerbale visVerb;
    public bool isIntroductory;
    public int VideobuttonClicks;
}

public class SlidesDataSender : MonoBehaviour
{
    [SerializeField] private RectTransform content;

    private Dictionary<string, SlideDataContainer> slidesData = new Dictionary<string, SlideDataContainer>();

    [SerializeField] SlidesDataRecorder slidesDataRecorder;

    [Header("Bottoni per slide visive")]
    [SerializeField] private List<Button> visualButtons;

    private string feedbackName;

    public string FeedbackName => feedbackName;

    public int FinalDataCount = 0;

    private bool allFinalDataSend = false;

    public float tempoOsservazionePreStep = 0f;

    private int _globalOpeningCounter = 0;

    public List<int> visitHistory = new List<int>();
    private Dictionary<string, int> _slideIndexMap = new Dictionary<string, int>();

    private int _totalButtonClicks = 0;


    void Start()
    {
        slidesDataRecorder = FindFirstObjectByType<SlidesDataRecorder>();

        feedbackName = transform.parent.name;

        if (content == null)
        {
            Debug.LogError("Missing reference: content");
            return;
        }

        int index = 0;
        foreach (Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
            {
                _slideIndexMap[child.name] = index;
                slide.OnSlideDataUpdated += SaveSlidesData;
                index++;
            }
        }

        RegisterVisualButtonListeners();
    }

    private void RegisterVisualButtonListeners()
    {
        foreach (var btn in visualButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(OnVisualButtonClicked);
        }
    }

    private void OnVisualButtonClicked()
    {
        _totalButtonClicks++;
        Debug.Log($"[VisualButton] Click totali: {_totalButtonClicks}");
    }

    public void SaveSlidesData(SlideDataContainer container)
    {
        SlideDataContainer data;

        if (string.IsNullOrEmpty(container.pageName))
            return;

        if (_slideIndexMap.TryGetValue(container.pageName, out int slideIndex) && !container.isIntroductory)
            visitHistory.Add(slideIndex);

        if (slidesData.TryGetValue(container.pageName, out data))
        {
            data.focusTime = container.focusTime;
            data.normalizedFocusTime = container.normalizedFocusTime;
            data.opening = container.opening;
            data.seqGlob = container.seqGlob;
            data.visVerb = container.visVerb;
        }
        else
        {
            slidesData.Add(container.pageName, container);
            Debug.Log($"Slide: {container.pageName} (index {slideIndex}) salvata nel sender");
        }
    }

    public void SendData()
    {
        if (slidesDataRecorder == null)
            return;

        if (!string.IsNullOrEmpty(feedbackName) && feedbackName.Contains("Introduzione"))
        {
            slidesData.Clear();
            visitHistory.Clear();
            return;
        }

        var introIndexes = slidesData.Values
            .Where(s => s.isIntroductory)
            .Select(s => _slideIndexMap.TryGetValue(s.pageName, out int idx) ? idx : -1)
            .Where(idx => idx >= 0)
            .ToHashSet();

        var filteredHistory = visitHistory
            .Where(idx => !introIndexes.Contains(idx))
            .ToList();

        int nonIntroCount = _slideIndexMap.Count - introIndexes.Count;

        if (filteredHistory.Count > nonIntroCount)
        {
            filteredHistory.RemoveRange(
                filteredHistory.Count - nonIntroCount,
                nonIntroCount
            );
        }

        float tempoTotale = slidesData.Values
            .Where(s => !s.isIntroductory)
            .Sum(s => s.focusTime);

        var filteredSlidesData = slidesData
            .Where(kvp => !kvp.Value.isIntroductory)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Distribuisci click sulle slide visive, -1 sulle verbali
        foreach (var s in filteredSlidesData.Values)
        {
            if (s.visVerb == LearningEnums.VisivoVerbale.Visivo)
                s.VideobuttonClicks = _totalButtonClicks;
            else
                s.VideobuttonClicks = -1;
        }

        _totalButtonClicks = 0;

        var copy = new Dictionary<string, SlideDataContainer>(filteredSlidesData);

        slidesDataRecorder.RecordData(
            feedbackName,
            copy,
            tempoOsservazionePreStep,
            filteredHistory,
            tempoTotale
        );

        slidesData.Clear();
        visitHistory.Clear();
    }

    private void OnDestroy()
    {
        foreach (Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
                slide.OnSlideDataUpdated -= SaveSlidesData;
        }

        foreach (var btn in visualButtons)
        {
            if (btn != null)
                btn.onClick.RemoveListener(OnVisualButtonClicked);
        }
    }

    public void SetTempoPreStep(float tempo)
    {
        tempoOsservazionePreStep = tempo;
    }

    public float GetCurrentTotalFocusTime()
    {
        float total = 0f;
        foreach (Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
            {
                Debug.Log($"[GetCurrentTotalFocusTime] Slide: {slide.pageName}, focusTime: {slide.getFocusTime()}");
                total += slide.getFocusTime();
            }
        }
        Debug.Log($"[GetCurrentTotalFocusTime] Totale: {total}");
        return total;
    }

    public float GetTotalFocusTime()
    {
        return slidesData.Values.Sum(s => s.focusTime);
    }
}