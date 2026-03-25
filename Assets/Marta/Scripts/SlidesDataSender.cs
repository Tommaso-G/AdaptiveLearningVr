using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SlideDataContainer
{
    public string pageName;
    public float focusTime;
    public int opening;
    public LearningEnums.SequenzialeGlobale seqGlob;
    public LearningEnums.VisivoVerbale visVerb;
}
public class SlidesDataSender : MonoBehaviour
{
    [SerializeField] private RectTransform content;

    private Dictionary<string, SlideDataContainer> slidesData = new Dictionary<string, SlideDataContainer>();

    [SerializeField] SlidesDataRecorder slidesDataRecorder;

    private string feedbackName;

    public int FinalDataCount = 0;

    private bool allFinalDataSend = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slidesDataRecorder = FindFirstObjectByType<SlidesDataRecorder>();

        feedbackName = transform.parent.name;

        if (content == null)
        {
            Debug.LogError("Missing reference: content");
            return;
        }

        foreach(Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
            {
                slide.OnSlideDataUpdated += SaveSlidesData;
            }
        }

    }

    public void SaveSlidesData(SlideDataContainer container)
    {
        SlideDataContainer data;

        if (string.IsNullOrEmpty(container.pageName))
        {
            return;
        }

        if (slidesData.TryGetValue(container.pageName, out data))
        {
            data.pageName = container.pageName;
            data.focusTime = container.focusTime;
            data.opening = container.opening;
            data.seqGlob = container.seqGlob;
            data.visVerb = container.visVerb;
        }
        else
        {
            slidesData.Add(container.pageName, container);
            Debug.Log("Dati della pagina: " + container.pageName + " salvati nel sender");
        }
    }
    public void SendData()
    {
        if (slidesDataRecorder != null)
        {
            var copy = new Dictionary<string, SlideDataContainer>(slidesData);
            slidesDataRecorder.RecordData(feedbackName, copy);
            slidesData.Clear();
        }
    }

    private void OnDestroy()
    {
        foreach (Transform child in content)
        {
            SlideData slide = child.GetComponent<SlideData>();
            if (slide != null)
            {
                slide.OnSlideDataUpdated -= SaveSlidesData;
            }
        }
    }
}
