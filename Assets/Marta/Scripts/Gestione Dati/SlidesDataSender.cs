using System.Collections.Generic;
using System.Linq;
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

public class ExtendedSlideDataContainer : SlideDataContainer
{
    /// <summary>Numero di volte che l'utente ha distolto e riportato lo sguardo (esclusa la prima).</summary>
    public int reGazeCount;
 
    /// <summary>Durata in secondi di ogni singola sessione di gaze.</summary>
    public List<float> gazeSessions;
 
    /// <summary>Media della durata delle sessioni di gaze.</summary>
    public float avgGazeSession;
 
    /// <summary>Durata massima di una singola sessione di gaze.</summary>
    public float maxGazeSession;
 
    /// <summary>
    /// Tempo in secondi dal primo gaze dell'utente sul pannello alla chiusura dell'ultimo step.
    /// -1 se l'utente non ha mai guardato il pannello.
    /// </summary>
    public float tempoDalPrimoSguardo;
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

    /// <summary>
    /// Time.time del primo GazeSelection su qualsiasi slide ExtendedSlideData del pannello.
    /// Rimane -1f se nessuna slide è Extended oppure l'utente non ha mai guardato.
    /// </summary>
    public float firstGazeTimestamp = -1f;

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

                // Sottoscrivi OnFirstGazeOnPanel solo se la slide è ExtendedSlideData
                if (slide is ExtendedSlideData extSlide)
                    extSlide.OnFirstGazeOnPanel += RegisterFirstGaze;

                index++;
            }
        }

        RegisterVisualButtonListeners();
    }

    /// <summary>
    /// Registra il timestamp del primo gaze sul pannello.
    /// Chiamato da qualsiasi ExtendedSlideData figlia al suo primo GazeSelection.
    /// Il controllo garantisce che solo il primissimo gaze tra tutte le slide conti.
    /// </summary>
    private void RegisterFirstGaze()
    {
        if (firstGazeTimestamp >= 0f) return;
        firstGazeTimestamp = Time.time;
        Debug.Log($"[SlidesDataSender] Primo gaze registrato per '{feedbackName}' a t={firstGazeTimestamp:F2}s");
    }

    private void RegisterVisualButtonListeners()
    {
        foreach (var btn in visualButtons)
            if (btn != null) btn.onClick.AddListener(OnVisualButtonClicked);
    }

    private void OnVisualButtonClicked()
    {
        _totalButtonClicks++;
        Debug.Log($"[VisualButton] Click totali: {_totalButtonClicks}");
    }

    public void SaveSlidesData(SlideDataContainer container)
    {
        Debug.Log(
            $"[SaveSlidesData] Ricevuta slide={container.pageName} " +
            $"focus={container.focusTime} normalized={container.normalizedFocusTime} " +
            $"opening={container.opening} intro={container.isIntroductory}"
        );

        if (string.IsNullOrEmpty(container.pageName))
        {
            Debug.LogWarning("[SaveSlidesData] pageName nullo o vuoto");
            return;
        }

        if (_slideIndexMap.TryGetValue(container.pageName, out int slideIndex))
        {
            if (!container.isIntroductory)
            {
                visitHistory.Add(slideIndex);
                Debug.Log($"[SaveSlidesData] Aggiunta visita slide {container.pageName} indice={slideIndex}. Storico={visitHistory.Count}");
            }
        }
        else
        {
            Debug.LogError($"[SaveSlidesData] Slide {container.pageName} non trovata in _slideIndexMap");
        }

        if (slidesData.TryGetValue(container.pageName, out SlideDataContainer data))
        {
            Debug.Log($"[SaveSlidesData] Aggiornamento dati slide {container.pageName}");
            data.focusTime = container.focusTime;
            data.normalizedFocusTime = container.normalizedFocusTime;
            data.opening = container.opening;
            data.seqGlob = container.seqGlob;
            data.visVerb = container.visVerb;

            // Aggiorna campi estesi se applicabile (no-op per SlideDataContainer normali)
            if (data is ExtendedSlideDataContainer extData &&
                container is ExtendedSlideDataContainer extContainer)
            {
                extData.reGazeCount    = extContainer.reGazeCount;
                extData.gazeSessions   = extContainer.gazeSessions;
                extData.avgGazeSession = extContainer.avgGazeSession;
                extData.maxGazeSession = extContainer.maxGazeSession;
            }
        }
        else
        {
            slidesData.Add(container.pageName, container);
            Debug.Log($"[SaveSlidesData] Nuova slide salvata: {container.pageName}. Totale={slidesData.Count}");
        }
    }

    /// <summary>
    /// Invia i dati al recorder. tempoChiusura è Time.time al momento della chiusura del pannello,
    /// usato per calcolare tempoDalPrimoSguardo nei container Extended.
    /// Se firstGazeTimestamp è -1 (slide non Extended o utente non ha guardato) passa -1 al recorder.
    /// </summary>
    public void SendData(float tempoChiusura)
    {
        if (slidesDataRecorder == null) return;

        if (!string.IsNullOrEmpty(feedbackName) && feedbackName.Contains("Introduzione"))
        {
            slidesData.Clear();
            visitHistory.Clear();
            firstGazeTimestamp = -1f;
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
            filteredHistory.RemoveRange(filteredHistory.Count - nonIntroCount, nonIntroCount);

        float tempoTotale = slidesData.Values
            .Where(s => !s.isIntroductory)
            .Sum(s => s.focusTime);

        var filteredSlidesData = slidesData
            .Where(kvp => !kvp.Value.isIntroductory)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var s in filteredSlidesData.Values)
        {
            if (s.visVerb == LearningEnums.VisivoVerbale.Visivo)
                s.VideobuttonClicks = _totalButtonClicks;
            else
                s.VideobuttonClicks = -1;
        }

        _totalButtonClicks = 0;

        // Calcola tempoDalPrimoSguardo solo se firstGazeTimestamp è stato registrato (slide Extended)
        float tempoDalPrimoSguardo = (firstGazeTimestamp >= 0f)
            ? tempoChiusura - firstGazeTimestamp
            : -1f;

        if (tempoDalPrimoSguardo >= 0f)
            Debug.Log($"[SlidesDataSender] tempoDalPrimoSguardo={tempoDalPrimoSguardo:F2}s per '{feedbackName}'");
        else
            Debug.Log($"[SlidesDataSender] tempoDalPrimoSguardo non disponibile per '{feedbackName}' (slide non Extended o utente non ha guardato)");

        // Propaga tempoDalPrimoSguardo nei container Extended
        foreach (var s in filteredSlidesData.Values)
        {
            if (s is ExtendedSlideDataContainer ext)
                ext.tempoDalPrimoSguardo = tempoDalPrimoSguardo;
        }

        var copy = new Dictionary<string, SlideDataContainer>(filteredSlidesData);

        slidesDataRecorder.RecordData(
            feedbackName,
            copy,
            tempoOsservazionePreStep,
            filteredHistory,
            tempoTotale,
            tempoDalPrimoSguardo
        );

        slidesData.Clear();
        visitHistory.Clear();
        firstGazeTimestamp = -1f;
    }

    // Overload senza tempoChiusura per compatibilità con chiamate esistenti
    public void SendData() => SendData(Time.time);

    private void OnDestroy()
    {
        Debug.Log($"DESTROY Sender {name}");
        foreach (Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
            {
                slide.OnSlideDataUpdated -= SaveSlidesData;
                if (slide is ExtendedSlideData extSlide)
                    extSlide.OnFirstGazeOnPanel -= RegisterFirstGaze;
            }
        }

        foreach (var btn in visualButtons)
            if (btn != null) btn.onClick.RemoveListener(OnVisualButtonClicked);
    }

    public void SetTempoPreStep(float tempo) { tempoOsservazionePreStep = tempo; }

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

    public float GetTotalFocusTime() => slidesData.Values.Sum(s => s.focusTime);
}