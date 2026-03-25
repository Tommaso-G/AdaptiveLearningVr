using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRBuilder.Core;

public static class ErrorEvent
{
    public static IProcess process { get; private set; }

    public static System.Action<string, string, string> OnError;

    public static void SetProcess(IProcess p)
    {
        process = p;
    }
}

[System.Serializable]
public class StepError
{
    public string wrongStepName;
    public string interactedObjectName;

    public StepError(string wrongStepName, string interactedObjectName)
    {
        this.wrongStepName = wrongStepName;
        this.interactedObjectName = interactedObjectName;
    }

    public override string ToString()
    {
        return $"[ERROR] Object: '{interactedObjectName}' | Wrong step: '{wrongStepName}'";
    }
}

[System.Serializable]
public class ChapterErrorData
{
    public string chapterName;
    public List<StepError> errors = new List<StepError>();
    public int TotalErrors => errors.Count;

    public ChapterErrorData(string chapterName)
    {
        this.chapterName = chapterName;
    }
}

public class StepErrorTracker : MonoBehaviour
{
    private Dictionary<string, ChapterErrorData> chapterErrors = new Dictionary<string, ChapterErrorData>();
    public int TotalErrors { get; private set; } = 0;
    public IReadOnlyDictionary<string, ChapterErrorData> ChapterErrors => chapterErrors;
    public TMP_Text textPanel;
    private void Start()
    {
        ErrorEvent.OnError += RegisterError;
    }

    // Da chiamare all'avvio del processo, per registrare tutti i capitoli
    public void InitializeChapters(IList<IChapter> chapters)
    {
        chapterErrors.Clear();
        TotalErrors = 0;
        foreach (IChapter chapter in chapters)
        {
            string name = chapter.Data.Name;
            if (!chapterErrors.ContainsKey(name))
            {
                chapterErrors[name] = new ChapterErrorData(name);
            }
        }
        Debug.Log($"[StepErrorTracker] Initialized with {chapterErrors.Count} chapters.");
    }

    public void RegisterError(string chapterName, string wrongStepName, string interactedObjectName)
    {
        if (!chapterErrors.ContainsKey(chapterName))
        {
            // Fallback: se il capitolo non è stato inizializzato, lo crea comunque
            chapterErrors[chapterName] = new ChapterErrorData(chapterName);
            Debug.LogWarning($"[StepErrorTracker] Chapter '{chapterName}' was not initialized. Created on the fly.");
        }

        StepError error = new StepError(wrongStepName, interactedObjectName);
        chapterErrors[chapterName].errors.Add(error);
        TotalErrors++;
        Debug.Log($"[StepErrorTracker] Chapter: '{chapterName}' | {error} | Total errors: {TotalErrors}");
    }

    public void Clear()
    {
        chapterErrors.Clear();
        TotalErrors = 0;
        Debug.Log("[StepErrorTracker] Error tracker cleared.");
    }

    public void PrintAll()
    {
        if (TotalErrors == 0)
        {
            Debug.Log("[StepErrorTracker] No errors recorded.");
            return;
        }

        foreach (var kvp in chapterErrors)
        {
            ChapterErrorData data = kvp.Value;
            Debug.Log($"--- Chapter: '{data.chapterName}' | Errors: {data.TotalErrors} ---");
            foreach (var e in data.errors)
            {
                Debug.Log($"   {e}");
            }
        }
    }

    public void UpdateErrorPanel()
    {
        if (textPanel == null)
        {
            Debug.LogWarning("[StepErrorTracker] textPanel non assegnato.");
            return;
        }

        if (TotalErrors == 0)
        {
            textPanel.text = "Nessun errore commesso.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Errori totali: {TotalErrors}");
        sb.AppendLine("─────────────────");

        foreach (var kvp in chapterErrors)
        {
            ChapterErrorData data = kvp.Value;
            if (data.TotalErrors > 0)
            {
                sb.AppendLine($"Capitolo '{data.chapterName}': {data.TotalErrors} errori");
            }
        }

        textPanel.text = sb.ToString();
    }
}