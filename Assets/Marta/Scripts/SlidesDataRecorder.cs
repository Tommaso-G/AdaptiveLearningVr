using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using System.Collections;
using System.Text;
using static FeedbackRepository;

public class FeedbackDataContainer
{
    public string feedbackName;
    public Dictionary<string, SlideDataContainer> slidesData = new Dictionary<string, SlideDataContainer>();
}
public class SlidesDataRecorder : MonoBehaviour
{

    private Dictionary<string, FeedbackDataContainer> FeedbacksDataList = new Dictionary<string, FeedbackDataContainer>();
    public void RecordData(string feedbackName, Dictionary<string, SlideDataContainer> slidesData)
    {
        FeedbackDataContainer feedbackData;

        foreach (var slideData in slidesData)
        {
            if (string.IsNullOrEmpty(slideData.Value.pageName))
            {
                return;
            }

            if (FeedbacksDataList.TryGetValue(feedbackName, out feedbackData))
            {
                feedbackData.slidesData = slidesData;
            }
            else
            {
                feedbackData = new FeedbackDataContainer();
                feedbackData.feedbackName = feedbackName;
                feedbackData.slidesData = slidesData;
                FeedbacksDataList.Add(feedbackName, feedbackData);
                Debug.Log("Dati del feedback: " + feedbackName + " salvati nel recorder");
            }
        }
    }

    private void printRecorderSavings()
    {
        if(FeedbacksDataList == null)
        {
            print("Feedback recorder vuoto");
            return;
        }

        foreach (var feedbackdata in FeedbacksDataList)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("FEEDBACK: " + feedbackdata.Value.feedbackName);

            foreach (var data in feedbackdata.Value.slidesData)
            {
                sb.AppendLine("Pagina: " + data.Value.pageName);
                sb.AppendLine("focus time: " + data.Value.focusTime);
                sb.AppendLine("opening: " + data.Value.opening);
                sb.AppendLine("globale/sequenziale: " + data.Value.seqGlob);
                sb.AppendLine("visivo/verbale: " + data.Value.visVerb);
                sb.AppendLine("----------------------");
            }

            Debug.Log(sb.ToString());
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            printRecorderSavings();
        }
    }
}