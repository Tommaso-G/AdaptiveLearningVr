using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    public string missedStepName;
    public string interactedObjectName;

    public StepError(string missedStepName, string interactedObjectName)
    {
        this.missedStepName = missedStepName;
        this.interactedObjectName = interactedObjectName;
    }

    public override string ToString()
    {
        return $"[ERROR] Object: '{interactedObjectName}' | Missed step: '{missedStepName}'";
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

    public TMP_Text textPanelOnHand;
    public HandMenuRequester handMenuRequester;
    public PersistentErrorLog errorLog;

    [System.Serializable]
    public class CustomErrorMessage
    {
        [Tooltip("Nome dell'oggetto interagito (deve corrispondere esattamente a interactedObjectName).")]
        public string interactedObjectName;
        [Tooltip("Messaggio personalizzato da mostrare su textPanelOnHand.")]
        [TextArea(2, 4)]
        public string customMessage;
    }

    [Header("Messaggi personalizzati")]
    [Tooltip("Se l'oggetto interagito corrisponde, usa il messaggio personalizzato invece di quello standard.")]
    public List<CustomErrorMessage> customErrorMessages = new List<CustomErrorMessage>();

    public IProcess CurrentProcess => ProcessRunner.Current;

    private LearningProfile learningProfile;

    private void Start()
    {
        ErrorEvent.OnError += RegisterError;
        learningProfile = GetComponent<LearningProfile>();
        if (learningProfile == null)
            learningProfile = FindFirstObjectByType<LearningProfile>();

        // Notifica il log persistente del profilo corrente
        // (gestisce reset automatico se il profilo è cambiato)
        if (SessionPersistence.GetResetAll())
        {
            if (errorLog != null)
                errorLog.resetOnNext = true;
        }

        string currentProfile = IsSequenziale() ? "Sequenziale" : "Globale";
        errorLog?.InitSession(currentProfile);
    }

    private void OnDestroy()
    {
        ErrorEvent.OnError -= RegisterError;
    }

    // ─────────────────────────────────────────────────────────────────
    // INIZIALIZZAZIONE
    // ─────────────────────────────────────────────────────────────────

    public void InitializeChapters(IList<IChapter> chapters)
    {
        chapterErrors.Clear();
        TotalErrors = 0;
        foreach (IChapter chapter in chapters)
        {
            string name = chapter.Data.Name;
            if (!chapterErrors.ContainsKey(name))
                chapterErrors[name] = new ChapterErrorData(name);
        }
        Debug.Log($"[StepErrorTracker] Initialized with {chapterErrors.Count} chapters.");
    }

    // ─────────────────────────────────────────────────────────────────
    // REGISTRAZIONE ERRORE
    // ─────────────────────────────────────────────────────────────────

    public void RegisterError(string chapterName, string missedStepName, string interactedObjectName)
    {
        if (!chapterErrors.ContainsKey(chapterName))
        {
            chapterErrors[chapterName] = new ChapterErrorData(chapterName);
            Debug.LogWarning($"[StepErrorTracker] Chapter '{chapterName}' was not initialized. Created on the fly.");
        }

        StepError error = new StepError(missedStepName, interactedObjectName);
        chapterErrors[chapterName].errors.Add(error);
        TotalErrors++;
        Debug.Log($"[StepErrorTracker] Chapter: '{chapterName}' | {error} | Total errors: {TotalErrors}");

        // Aggiorna i pannelli in base al profilo
        if (IsSequenziale())
            UpdatePanelsSequenziale(error);
        // Per Globale: i pannelli si aggiornano solo a fine capitolo, tramite NotifyChapterCompleted()
    }

    // ─────────────────────────────────────────────────────────────────
    // NOTIFICA FINE CAPITOLO (solo per Globale)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Da chiamare quando un capitolo termina.
    /// In modalità Globale aggiorna textPanelOnHand con il riepilogo del capitolo appena concluso.
    /// </summary>
    public void NotifyChapterCompleted(string completedChapterName)
    {
        if (!IsSequenziale())
            UpdatePanelsGlobale(completedChapterName);
    }

    // ─────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO PANNELLI — SEQUENZIALE
    // ─────────────────────────────────────────────────────────────────

    private void UpdatePanelsSequenziale(StepError lastError)
    {
        // textPanelOnHand: solo l'ultimo errore
        if (textPanelOnHand != null)
        {
            CustomErrorMessage custom = customErrorMessages.Find(c =>
                string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

            textPanelOnHand.text = custom != null
                ? custom.customMessage
                : $"Ora non è il momento di interagire con {lastError.interactedObjectName} " +
                  $"devi invece eseguire lo step {lastError.missedStepName}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(textPanelOnHand.rectTransform);
            handMenuRequester?.OpenMenu();
        }

        // errorLog persistente: diviso in Step dimenticati ed Errori commessi
        var sb = new System.Text.StringBuilder();

        var stepDimenticati = new List<string>();
        var erroriCommessi = new List<string>();

        foreach (var kvp in chapterErrors)
        {
            foreach (var error in kvp.Value.errors)
            {
                CustomErrorMessage custom = customErrorMessages.Find(c =>
                    string.Equals(c.interactedObjectName, error.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

                if (custom != null)
                    erroriCommessi.Add(custom.customMessage);
                else
                    stepDimenticati.Add(error.missedStepName);
            }
        }

        sb.AppendLine("Step dimenticati");
        sb.AppendLine("─────────────────");
        if (stepDimenticati.Count > 0)
            foreach (var s in stepDimenticati)
                sb.AppendLine($"• {s}");
        else
            sb.AppendLine("Nessuno");

        sb.AppendLine();
        sb.AppendLine("Errori commessi");
        sb.AppendLine("─────────────────");
        if (erroriCommessi.Count > 0)
            foreach (var e in erroriCommessi)
                sb.AppendLine($"• {e}");
        else
            sb.AppendLine("Nessuno");

        errorLog?.UpdateText(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO PANNELLI — GLOBALE
    // ─────────────────────────────────────────────────────────────────

    private void UpdatePanelsGlobale(string completedChapterName)
    {
        // textPanelOnHand: nome capitolo appena concluso + step mancati in quel capitolo
        if (textPanelOnHand != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(completedChapterName);
            sb.AppendLine("─────────────────");

            if (chapterErrors.TryGetValue(completedChapterName, out ChapterErrorData data) && data.TotalErrors > 0)
            {
                foreach (var error in data.errors)
                    sb.AppendLine($"• {error.missedStepName}");
            }
            else
            {
                sb.AppendLine("Nessun errore commesso.");
            }

            textPanelOnHand.text = sb.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(textPanelOnHand.rectTransform);
            handMenuRequester?.OpenMenu();
        }

        // errorLog persistente: per capitolo, conteggio Step dimenticati ed Errori commessi
        var sbLog = new System.Text.StringBuilder();

        foreach (var kvp in chapterErrors)
        {
            ChapterErrorData data = kvp.Value;

            int stepDimenticati = 0;
            int erroriCommessi = 0;

            foreach (var error in data.errors)
            {
                CustomErrorMessage custom = customErrorMessages.Find(c =>
                    string.Equals(c.interactedObjectName, error.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

                if (custom != null)
                    erroriCommessi++;
                else
                    stepDimenticati++;
            }

            sbLog.AppendLine(data.chapterName);
            sbLog.AppendLine($"  Step dimenticati: {stepDimenticati}");
            sbLog.AppendLine($"  Errori commessi: {erroriCommessi}");
            sbLog.AppendLine();
        }

        errorLog?.UpdateText(sbLog.ToString());
    }

    // ─────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────

    private bool IsSequenziale()
    {
        if (learningProfile == null) return true; // fallback sicuro
        return learningProfile.GetProfileTuple().sequenzialeGlobale
               == LearningEnums.SequenzialeGlobale.Sequenziale;
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
                Debug.Log($"   {e}");
        }
    }
}