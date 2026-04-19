using UnityEngine;
using System;

public class EventTimer : MonoBehaviour
{
    private bool isRunning;
    private float elapsedTime;

    public void Start()
    {
        isRunning = true;
        StartTimer();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetTime()
    {
        return elapsedTime;
    }

    void Update()
    {
        if (isRunning)
            elapsedTime += Time.deltaTime;
    }
}