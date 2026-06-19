using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Estende SlideData aggiungendo:
/// - tracciamento sessioni di gaze (reGazeCount, gazeSessions)
/// - evento OnFirstGazeOnPanel: fired una sola volta al primo GazeSelection
/// </summary>
public class ExtendedSlideData : SlideData
{
    private int reGazeCount = 0;
    private List<float> gazeSessions = new List<float>();
    private float gazeSessionStart = 0f;
    private bool gazeActive = false;
    private bool hasBeenSeenAtLeastOnce = false;

    // Fired una sola volta: al primo GazeSelection su questa slide
    public event System.Action OnFirstGazeOnPanel;
    private bool firstGazeFired = false;

    public int ReGazeCount => reGazeCount;
    public IReadOnlyList<float> GazeSessions => gazeSessions.AsReadOnly();

    public override void GazeSelection()
    {
        // Notifica il primo gaze (una volta sola per tutta la sessione)
        if (!firstGazeFired)
        {
            firstGazeFired = true;
            OnFirstGazeOnPanel?.Invoke();
        }

        if (hasBeenSeenAtLeastOnce)
            reGazeCount++;
        else
            hasBeenSeenAtLeastOnce = true;

        gazeSessionStart = Time.time;
        gazeActive = true;

        base.GazeSelection();
    }

    public override void GazeDeselection()
    {
        if (gazeActive)
        {
            float sessionDuration = Time.time - gazeSessionStart;
            gazeSessions.Add(sessionDuration);
            gazeActive = false;

            Debug.Log($"[ExtendedSlideData] Sessione gaze terminata su {pageName}: " +
                      $"durata={sessionDuration:F2}s, sessioni={gazeSessions.Count}, reGaze={reGazeCount}");
        }

        base.GazeDeselection();
    }

    public float GetAverageGazeSessionDuration()
    {
        if (gazeSessions.Count == 0) return 0f;
        float sum = 0f;
        foreach (float s in gazeSessions) sum += s;
        return sum / gazeSessions.Count;
    }

    public float GetMaxGazeSessionDuration()
    {
        if (gazeSessions.Count == 0) return 0f;
        float max = 0f;
        foreach (float s in gazeSessions) if (s > max) max = s;
        return max;
    }

    public override void SendData()
    {
        // Chiude la sessione attiva se il pannello viene disabilitato mentre lo sguardo era sul pannello
        if (gazeActive)
        {
            float sessionDuration = Time.time - gazeSessionStart;
            gazeSessions.Add(sessionDuration);
            gazeActive = false;
        }

        var container = new ExtendedSlideDataContainer
        {
            pageName            = pageName,
            focusTime           = getFocusTime(),
            normalizedFocusTime = GetNormalizedFocusTime(),
            opening             = getOpening(),
            seqGlob             = seqGlob,
            visVerb             = visVerb,
            isIntroductory      = isIntroductory,
            reGazeCount         = reGazeCount,
            gazeSessions        = new List<float>(gazeSessions),
            avgGazeSession      = GetAverageGazeSessionDuration(),
            maxGazeSession      = GetMaxGazeSessionDuration()
        };

        Debug.Log($"[ExtendedSlideData] Invoco OnSlideDataUpdated per {pageName} " +
                  $"reGazeCount={reGazeCount} sessioni={gazeSessions.Count}");

        InvokeOnSlideDataUpdated(container);
    }
}