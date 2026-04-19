using System.Diagnostics;
using UnityEngine;
using VRBuilder.Core;

[System.Serializable]
public class MinigameDataContainer
{
    public string minigameChapterName;
    public float completitionTime = 0f;
    public int errors = 0;
    public int moves = 0;
}
public class MinigameDataSender : MonoBehaviour
{
    private float startTime;
    private int errorCount;
    private bool isRunning;
    private int moveCount;

    public bool countMoves = false;
    public bool countErrors = false;

    private IProcess process;

    private MinigameDataContainer container = new MinigameDataContainer();
    private MinigameDataRecorder recorder;

    private void Start()
    {
        recorder = FindFirstObjectByType<MinigameDataRecorder>();
        UnityEngine.Debug.Log(recorder);
        
    }
    public void StartTracking()
    {
        print("Start Traking");
        if (ProcessRunner.Current != null)
        {
            process = ProcessRunner.Current;
            container.minigameChapterName = process.Data.Current.Data.Name;
            if (container.minigameChapterName != null)
            {
                print("Assegnato il nome: " + container.minigameChapterName);
            }
            else
            {
                print("viviamo in un mondo di perdizione");
            }
        }
        startTime = Time.time;

        if (!countErrors)
        {
            errorCount = -1;
        }
        else
        {
            errorCount = 0;
        }

        if (!countMoves)
        {
            moveCount = -1;
        }
        else
        {
            moveCount = 0;
        }

        isRunning = true;
    }

    public void AddError()
    {
        if (!isRunning || errorCount == -1) return;
        errorCount++;
    }

    public void AddMove()
    {
        if(!isRunning || moveCount == -1) return;
        moveCount++;
    }

    public void SendData()
    {
        if(recorder != null)
        {
            recorder.RecordData(container);
        }
    }

    public void Complete()
    {
        if (!isRunning) return;

        isRunning = false;
        container.completitionTime = Time.time - startTime;
        container.errors = errorCount;
        container.moves = moveCount;
        SendData();
    }
}
