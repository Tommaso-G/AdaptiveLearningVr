using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using VRBuilder.Core;

public static class ErrorEvent
{
    public static IProcess process { get; private set; }

    public static System.Action<string, string, string, string> OnError;

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

    [System.Serializable]
    public class CustomErrorMessageToChapter
    {
        [Tooltip("Nome del capitolo con errori custom.")]
        [ChapterDropdown]
        public string chapterName;
        [Tooltip("Lista errori custom per il capitolo.")]
        public List<CustomErrorMessage> customErrorMessages = new List<CustomErrorMessage>();


    }

    [Header("Messaggi personalizzati")]
    [Tooltip("Se l'oggetto interagito corrisponde, usa il messaggio personalizzato invece di quello standard.")]
    public List<CustomErrorMessage> customErrorMessages = new List<CustomErrorMessage>();

    [Header("Messaggi personalizzati Runtime")]
    public List<CustomErrorMessage> customErrorMessagesRuntime = new List<CustomErrorMessage>();


    [Header("Messaggi personalizzati per il capitolo indicato")]
    [Tooltip("Inserire oggetti che hanno bisogno di un errore custom specifico se interagiti durante il capitolo indicato")]
    public List<CustomErrorMessageToChapter> customErrorMessagesToChapters = new List<CustomErrorMessageToChapter>();

    public IProcess CurrentProcess => ProcessRunner.Current;

    private LearningProfile learningProfile;

    private void Start()
    {
        ErrorEvent.OnError += RegisterError;
        learningProfile = GetComponent<LearningProfile>();
        if (learningProfile == null)
            learningProfile = FindFirstObjectByType<LearningProfile>();

        if (SessionPersistence.GetResetAll())
        {
            if (errorLog != null)
                errorLog.resetOnNext = true;
        }

        if (SessionManager.Instance == null || !SessionManager.Instance.IsOffline)
        {
            string currentProfile = IsSequenziale() ? "Sequenziale" : "Globale";
            errorLog?.InitSession(currentProfile);
        }
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

    public void RegisterError(string chapterName, string missedStepName, string interactedObjectName, string customRuntime = "")
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

        // Il pannello a mano si aggiorna sempre, indipendentemente dal profilo
        UpdatePanelOnHand(error, customRuntime, chapterName);

        // L'errorLog si aggiorna in base al profilo
        if (IsSequenziale())
            UpdatePanelsSequenziale(error, customRuntime, chapterName);
        // Per Globale: l'errorLog si aggiorna solo a fine capitolo, tramite NotifyChapterCompleted()
    }

    // ─────────────────────────────────────────────────────────────────
    // NOTIFICA FINE CAPITOLO (solo per Globale)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Da chiamare quando un capitolo termina.
    /// In modalità Globale aggiorna l'errorLog con il riepilogo del capitolo appena concluso.
    /// </summary>
    public void NotifyChapterCompleted(string completedChapterName)
    {
        if (!IsSequenziale())
            UpdatePanelsGlobale(completedChapterName);
    }

    // ─────────────────────────────────────────────────────────────────
    // PANNELLO A MANO — comune a entrambe le modalità
    // ─────────────────────────────────────────────────────────────────

    private void UpdatePanelOnHand(StepError lastError, string customRuntime, string chapterName)
    {
        if (textPanelOnHand == null) return;

        CustomErrorMessage custom = customErrorMessages.Find(c =>
            string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

        if (custom == null)
        {
            custom = customErrorMessagesRuntime.Find(c =>
                string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

            if (custom != null)
                custom = CustomErrorMessageRuntime(custom, customRuntime);
        }


        if (custom == null)
        {
            CustomErrorMessageToChapter chapterWithCustom = customErrorMessagesToChapters.Find(c => string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

            if (chapterWithCustom != null)
            {
                custom = chapterWithCustom.customErrorMessages.Find(c =>
                    string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        textPanelOnHand.text = custom != null
            ? custom.customMessage
            : $"Ora non è il momento di interagire con <b><color=#00AAFF>{lastError.interactedObjectName}</color></b> " +
              $"devi invece eseguire lo step <b><color=#00AAFF>{lastError.missedStepName}</color></b>";

        LayoutRebuilder.ForceRebuildLayoutImmediate(textPanelOnHand.rectTransform);
        handMenuRequester?.OpenMenu();
    }

    // ─────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO ERRORLOG — SEQUENZIALE
    // ─────────────────────────────────────────────────────────────────

    private void UpdatePanelsSequenziale(StepError lastError, string customRuntime, string chapterName)
    {
        var sb = new System.Text.StringBuilder();

        var stepDimenticati = new Dictionary<string, int>();
        var erroriCommessi = new Dictionary<string, int>();

        foreach (var kvp in chapterErrors)
        {
            foreach (var error in kvp.Value.errors)
            {
                CustomErrorMessage custom = customErrorMessages.Find(c =>
                    string.Equals(c.interactedObjectName, error.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

                if (custom == null)
                {
                    custom = customErrorMessagesRuntime.Find(c =>
                        string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));

                    if (custom != null)
                        custom = CustomErrorMessageRuntime(custom, customRuntime);
                }

                if (custom == null)
                {
                    CustomErrorMessageToChapter chapterWithCustom = customErrorMessagesToChapters.Find(c => string.Equals(c.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase));

                    if (chapterWithCustom != null)
                    {
                        custom = chapterWithCustom.customErrorMessages.Find(c =>
                            string.Equals(c.interactedObjectName, lastError.interactedObjectName, System.StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (custom != null)
                {
                    if (!erroriCommessi.ContainsKey(custom.customMessage))
                        erroriCommessi[custom.customMessage] = 0;

                    erroriCommessi[custom.customMessage]++;
                }
                else
                {
                    if (!stepDimenticati.ContainsKey(error.missedStepName))
                        stepDimenticati[error.missedStepName] = 0;

                    stepDimenticati[error.missedStepName]++;
                }
            }
        }

        sb.AppendLine("Step dimenticati");
        sb.AppendLine("─────────────────");
        if (stepDimenticati.Count > 0)
            foreach (var kvp in stepDimenticati)
            {
                string label = kvp.Value > 1
                    ? $"• {kvp.Key} (x{kvp.Value})"
                    : $"• {kvp.Key}";
                sb.AppendLine(label);
            }
        else
            sb.AppendLine("Nessuno");

        sb.AppendLine();
        sb.AppendLine("Errori commessi");
        sb.AppendLine("─────────────────");
        if (erroriCommessi.Count > 0)
            foreach (var kvp in erroriCommessi)
            {
                string label = kvp.Value > 1
                    ? $"• {kvp.Key} (x{kvp.Value})"
                    : $"• {kvp.Key}";
                sb.AppendLine(label);
            }
        else
            sb.AppendLine("Nessuno");

        errorLog?.UpdateText(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO ERRORLOG — GLOBALE
    // ─────────────────────────────────────────────────────────────────

    private void UpdatePanelsGlobale(string completedChapterName)
    {
        var sbLog = new System.Text.StringBuilder();

        foreach (var kvp in chapterErrors)
        {
            ChapterErrorData data = kvp.Value;

            if (data.TotalErrors == 0) continue;

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
        if (learningProfile == null) return true;
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

    public CustomErrorMessage CustomErrorMessageRuntime(CustomErrorMessage custom, string custom_word)
    {
        if (string.IsNullOrEmpty(custom_word)) return null;

        string new_message = custom.customMessage.Replace("_", custom_word);

        CustomErrorMessage new_custom = new CustomErrorMessage();
        string id = System.Guid.NewGuid().ToString();
        new_custom.interactedObjectName = $"{custom.interactedObjectName}_custom[{id}]";
        new_custom.customMessage = new_message;

        return new_custom;
    }
}