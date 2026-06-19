using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEngine.Video;

public class SlideData : MonoBehaviour
{
    [SerializeField] SlidesDataSender sender;

    public string pageName;
    private float focusTime;
    public int opening;
    public LearningEnums.SequenzialeGlobale seqGlob;
    public LearningEnums.VisivoVerbale visVerb;
    public bool isIntroductory = false;
    public int wordCount;
    [SerializeField] private TMP_Text slideText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float videoDuration = 0f;

    public bool stopTimer = false;
    private Coroutine activeCoroutine = null;
    private float t = 0f;
    private bool wasEnable = false;
    private bool nameSet = false;

    [SerializeField] TMP_Text focusTimeTxt;

    public event Action<SlideDataContainer> OnSlideDataUpdated;

    public void setFocusTime(float t) { focusTime += t; }
    public void setOpening() { opening += 1; }
    public float getFocusTime() { return focusTime; }
    public int getOpening() { return opening; }
    public string getNamePage() { return pageName; }

    public void setLearningEnums(LearningEnums.SequenzialeGlobale sg, LearningEnums.VisivoVerbale vv)
    {
        seqGlob = sg;
        visVerb = vv;
    }

    public void setIntrodactoryField(bool iI) { isIntroductory = iI; }

    public IEnumerator StartTimer()
    {
        focusTimeTxt.text = "0";
        if (!wasEnable) { setOpening(); wasEnable = true; }
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
    }

    public virtual void GazeSelection()
    {
        if (activeCoroutine != null) return;
        stopTimer = false;
        activeCoroutine = StartCoroutine(StartTimer());
        if (!wasEnable) { opening++; wasEnable = true; }
    }

    public virtual void GazeDeselection()
    {
        if (activeCoroutine == null) return;
        stopTimer = true;
    }

    private void OnDisable()
    {
        if (!nameSet) { pageName = gameObject.name; nameSet = true; }
        Debug.Log("OnDisable chiamato");
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
        if (videoPlayer == null) { Debug.LogWarning($"[SlideData] videoPlayer non assegnato su {gameObject.name}"); return videoDuration; }
        if (videoPlayer.clip == null) { Debug.LogWarning($"[SlideData] clip nullo su {gameObject.name}"); return videoDuration; }
        return (float)videoPlayer.clip.length;
    }

    public float GetNormalizedFocusTime()
    {
        if (visVerb == LearningEnums.VisivoVerbale.Visivo)
        {
            float duration = GetVideoDuration();
            if (duration <= 0f) return focusTime;
            return focusTime / duration;
        }
        else
        {
            int wc = GetWordCount();
            if (wc == 0) return focusTime;
            return focusTime / wc;
        }
    }

    protected void InvokeOnSlideDataUpdated(SlideDataContainer container)
    {
        if (OnSlideDataUpdated == null)
        {
            Debug.LogWarning($"[SlideData] Nessun listener iscritto a {pageName}");
            return;
        }
        OnSlideDataUpdated.Invoke(container);
    }

    public virtual void SendData()
    {
        var container = new SlideDataContainer
        {
            pageName = pageName,
            focusTime = focusTime,
            normalizedFocusTime = GetNormalizedFocusTime(),
            opening = opening,
            seqGlob = seqGlob,
            visVerb = visVerb,
            isIntroductory = isIntroductory
        };

        if (OnSlideDataUpdated == null)
        {
            Debug.LogWarning($"[SlideData] Nessun listener iscritto a {pageName}");
            return;
        }

        Debug.Log($"[SlideData] Invoco OnSlideDataUpdated per {pageName}");
        OnSlideDataUpdated.Invoke(container);
    }
}