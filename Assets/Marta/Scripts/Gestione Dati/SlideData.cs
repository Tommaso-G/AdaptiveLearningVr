using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEngine.Video;

public class SlideData: MonoBehaviour
{
    [SerializeField] SlidesDataSender sender;

    //dati della slide
    public string pageName;
    private float focusTime;
    public int opening;
    public LearningEnums.SequenzialeGlobale seqGlob;
    public LearningEnums.VisivoVerbale visVerb;
    public bool isIntroductory = false;
    public int wordCount;
    [SerializeField] private TMP_Text slideText;

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float videoDuration = 0f; // fallback manuale


    //variabili di servizio
    public bool stopTimer = false;
    private Coroutine activeCoroutine = null;
    private float t = 0f;
    private bool wasEnable = false;
    private bool nameSet = false;

    //Debug
    [SerializeField] TMP_Text focusTimeTxt;

    public event Action<SlideDataContainer> OnSlideDataUpdated;

    public void setFocusTime(float t)
    {
        focusTime += t;
    }
    public void setOpening()
    {
        opening += 1;
    }
    public float getFocusTime()
    {
        return focusTime;
    }
    public int getOpening()
    {
        return opening;
    }
    public string getNamePage()
    {
        return pageName;
    }

    public void setLearningEnums (LearningEnums.SequenzialeGlobale sg, LearningEnums.VisivoVerbale vv)
    {
        seqGlob = sg;
        visVerb = vv;
        //Debug.Log("Impostati per pagina: " + pageName + " - " + seqGlob + " " + visVerb);
    }

    public void setIntrodactoryField(bool iI)
    {
        isIntroductory = iI;
    }
    public IEnumerator StartTimer()
    {
        focusTimeTxt.text = "0";

        if (!wasEnable)
        {
            setOpening();
            wasEnable = true;
        }

        t = Time.time;

        while (!stopTimer)
        {
            focusTimeTxt.text = (Time.time - t).ToString("F2");
            yield return null;
        }

        t = Time.time - t;
        setFocusTime(t);
        activeCoroutine = null;
        print("Aggiunti [" + t + "] alla pagina [" + pageName + "] ");
        ////Debug.Log("Tempo di focus per " + transform.name + ": " + focusTime + "\nopening: " + opening + "\n(coroutine counter: " + coroutineCounter + ")");
    }

    public void GazeSelection()
    {
        if (activeCoroutine != null) return;

        stopTimer = false;
        activeCoroutine = StartCoroutine(StartTimer());

        if (!wasEnable)
        {
            opening++;
            wasEnable = true;
        }
    }

    public void GazeDeselection()
    {
        if (activeCoroutine == null) return;

        stopTimer = true;
    }

    private void OnDisable()
    {
        if (!nameSet)
        {
            pageName = gameObject.name;
            nameSet = true;
        }

        if (activeCoroutine != null)
        {
            stopTimer = true;
            t = Time.time - t;
            setFocusTime(t);
            activeCoroutine = null;
            print("(ONDISABLE) Aggiunti [" + t + "] alla pagina [" + pageName + "] ");
        }

        wasEnable = false;

        SendData();

    }

    public int GetWordCount()
    {
        if (slideText == null) { wordCount = 0; return 0; }
        if (string.IsNullOrWhiteSpace(slideText.text)) { wordCount = 0; return 0; }
        wordCount = slideText.text.Split(new char[] { ' ', '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries).Length;
        return wordCount;
    }

    private float GetVideoDuration()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning($"[SlideData] videoPlayer non assegnato su {gameObject.name}");
            return videoDuration;
        }

        if (videoPlayer.clip == null)
        {
            Debug.LogWarning($"[SlideData] clip nullo su {gameObject.name}");
            return videoDuration;
        }

        return (float)videoPlayer.clip.length;
    }
    public float GetNormalizedFocusTime()
    {
        if (visVerb == LearningEnums.VisivoVerbale.Visivo)
        {
            float duration = GetVideoDuration();
            if (duration <= 0f){
//                Debug.Log("duration <0 ");
                return focusTime;

                } 
            return focusTime / duration;
        }

        else // Verbale
        {
            int wc = GetWordCount();
            if (wc == 0) return focusTime;
            return focusTime / wc;
        }
    }

    public void SendData()
    {
        OnSlideDataUpdated?.Invoke(new SlideDataContainer
        {
            pageName = pageName,
            focusTime = focusTime,
            normalizedFocusTime = GetNormalizedFocusTime(),
            opening = opening,
            seqGlob = seqGlob,
            visVerb = visVerb,
            isIntroductory = isIntroductory
        });
    }
}

