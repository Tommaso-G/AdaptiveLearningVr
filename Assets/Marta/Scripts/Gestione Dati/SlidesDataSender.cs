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
    public int buttonClickCount; // ← nuovo
}

public class SlidesDataSender : MonoBehaviour
{
    [SerializeField] private RectTransform content;

    private Dictionary<string, SlideDataContainer> slidesData = new Dictionary<string, SlideDataContainer>();

    [SerializeField] SlidesDataRecorder slidesDataRecorder;

    [Header("Bottoni per slide visive")]
    [SerializeField] private List<Button> visualButtons; // ← nuovo

    private string feedbackName;

    public string FeedbackName => feedbackName;

    public int FinalDataCount = 0;

    private bool allFinalDataSend = false;

    public float tempoOsservazionePreStep = 0f;

    private int _globalOpeningCounter = 0;

    public List<int> visitHistory = new List<int>();
    private Dictionary<string, int> _slideIndexMap = new Dictionary<string, int>();

    private int _currentButtonClickCount = 0; // ← nuovo
    private string _currentVisualSlideName = null; // ← nuovo


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

    // ← nuovo
    private void RegisterVisualButtonListeners()
    {
        foreach (var btn in visualButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(OnVisualButtonClicked);
        }
    }

    // ← nuovo
    private void OnVisualButtonClicked()
    {
        if (_currentVisualSlideName != null)
        {
            _currentButtonClickCount++;
            Debug.Log($"[VisualButton] Click #{_currentButtonClickCount} su slide visiva: {_currentVisualSlideName}");
        }
    }

    public void SaveSlidesData(SlideDataContainer container)
    {
        SlideDataContainer data;

        if (string.IsNullOrEmpty(container.pageName))
            return;

        // Se la slide corrente è visiva, salva i click accumulati e resetta
        if (_currentVisualSlideName != null &&
            _currentVisualSlideName != container.pageName &&
            slidesData.TryGetValue(_currentVisualSlideName, out var prevData))
        {
            prevData.buttonClickCount = _currentButtonClickCount;
            _currentButtonClickCount = 0;
        }

        // Traccia la slide visiva attiva
        if (container.visVerb == LearningEnums.VisivoVerbale.Visivo && !container.isIntroductory)
        {
            _currentVisualSlideName = container.pageName;
        }
        else
        {
            _currentVisualSlideName = null;
            _currentButtonClickCount = 0;
        }

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

        // Flush click count sull'ultima slide visiva prima di inviare
        if (_currentVisualSlideName != null &&
            slidesData.TryGetValue(_currentVisualSlideName, out var lastVisual))
        {
            lastVisual.buttonClickCount = _currentButtonClickCount;
            _currentButtonClickCount = 0;
            _currentVisualSlideName = null;
        }

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