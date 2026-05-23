using UnityEngine;
using VRBuilder.Core;

/// <summary>
/// Da assegnare all'oggetto snappabile.
/// Traccia se l'oggetto ha toccato il collider proibito e,
/// su richiesta, registra l'errore tramite ErrorEvent e fa lampeggiare l'oggetto.
/// Chiama CheckAndReportError() dal tuo behaviour OnSnap.
/// </summary>
public class WrongTouchErrorReporter : MonoBehaviour
{
    [Header("Configurazione")]
    [Tooltip("Il collider che non deve essere toccato prima dello snap.")]
    public Collider forbiddenCollider;

    [Tooltip("Nome dell'oggetto da passare all'errore.")]
    public string objectName = "";

    [Tooltip("Oggetto da far lampeggiare in caso di errore. (Vuoto se non serve)")]
    public GameObject objectToFlash;

    [Header("Debug")]
    public bool debugMode = false;

    bool m_TouchedForbidden = false;
    StepErrorTracker m_ErrorTracker;
    ExecutionOrderController m_ExecutionOrderController;

    void Start()
    {
        m_ErrorTracker = FindAnyObjectByType<StepErrorTracker>();
        m_ExecutionOrderController = FindAnyObjectByType<ExecutionOrderController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other != forbiddenCollider) return;

        m_TouchedForbidden = true;

        if (debugMode)
            Debug.Log($"[WrongTouchErrorReporter] '{name}' ha toccato '{other.name}'.", this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != forbiddenCollider) return;

        m_TouchedForbidden = true;

        if (debugMode)
            Debug.Log($"[WrongTouchErrorReporter] '{name}' ha colpito '{collision.collider.name}'.", this);
    }

    /// <summary>
    /// Chiama questa funzione nel tuo behaviour OnSnap.
    /// Se l'oggetto ha toccato il collider proibito, registra l'errore,
    /// fa lampeggiare l'oggetto e resetta il flag.
    /// </summary>
    public void CheckAndReportError()
    {
        if (!m_TouchedForbidden) return;

        string chapterName = GetCurrentChapterName();
        string stepName = GetCurrentStepName();

        if (m_ErrorTracker == null)
        {
            Debug.LogWarning("[WrongTouchErrorReporter] StepErrorTracker non trovato.", this);
            return;
        }

        if (stepName == null || objectName == null)
        {
            Debug.LogWarning("[WrongTouchErrorReporter] Step name o object name == null.", this);
            return;
        }

        m_ErrorTracker.RegisterError(chapterName, stepName, objectName);

        if (objectToFlash != null && m_ExecutionOrderController != null)
            m_ExecutionOrderController.DifferentStepWarningHighlight(objectToFlash);

        m_TouchedForbidden = false;

        if (debugMode)
            Debug.Log($"[WrongTouchErrorReporter] Errore registrato per '{objectName}' | capitolo: '{chapterName}' | step: '{stepName}'.", this);
    }

    /// <summary>
    /// Resetta il flag senza registrare errori.
    /// Utile quando l'oggetto viene riposizionato (es. ObjectBoundsKeeper).
    /// </summary>
    public void ResetTouchFlag()
    {
        m_TouchedForbidden = false;

        if (debugMode)
            Debug.Log($"[WrongTouchErrorReporter] Flag resettato per '{name}'.", this);
    }

    IChapter GetCurrentChapter()
    {
        IProcess process = ErrorEvent.process ?? ProcessRunner.Current;

        if (process == null)
        {
            Debug.LogWarning("[WrongTouchErrorReporter] Nessun processo trovato.", this);
            return null;
        }

        IChapter chapter = process.Data.Current;

        if (chapter == null)
            Debug.LogWarning("[WrongTouchErrorReporter] Nessun capitolo corrente trovato.", this);

        return chapter;
    }

    string GetCurrentChapterName()
    {
        IChapter chapter = GetCurrentChapter();
        return chapter != null ? chapter.Data.Name : "";
    }

    string GetCurrentStepName()
    {
        IChapter chapter = GetCurrentChapter();
        if (chapter == null) return null;

        IStep step = chapter.Data.Current;
        if (step == null)
        {
            Debug.LogWarning("[WrongTouchErrorReporter] Nessuno step corrente trovato.", this);
            return null;
        }

        return step.Data.Name;
    }
}