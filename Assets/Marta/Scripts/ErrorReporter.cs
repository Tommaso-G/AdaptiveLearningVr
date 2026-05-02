using UnityEngine;
using VRBuilder.Core;

public class ErrorReporter : MonoBehaviour
{
    [Header("A quale capitolo registrare gli errori")]
    [ChapterDropdown]
    public string chapterErrorName;

    [Header("Oggetto da illuminare in caso di errore.\n(Vuoto se non serve)")]
    public GameObject objectToFlash;

    public IProcess process = ProcessRunner.Current;

    private StepErrorTracker errorTracker;
    private ExecutionOrderController executionOrderController;

    void Start()
    {
        errorTracker = FindAnyObjectByType<StepErrorTracker>();
        executionOrderController = FindAnyObjectByType<ExecutionOrderController>();
    }

    public void RegisterError(string obj)
    {
        if (process == null)
        {
            process = ProcessRunner.Current;
        }
        
        string stepName = process.Data.Current.Data.Current.Data.Name;

        if (errorTracker == null)
        {
            print($"[ErrorReporter] tentativo registrazione errore fallito per il capitolo {chapterErrorName}.\nStepErrorTracker non trovato");
            return;
        }

        if(stepName == null || obj == null)
        {
            print($"[ErrorReporter] tentativo registrazione errore fallito per il capitolo {chapterErrorName}.\nStep name o object name == null");
            return;
        }
        errorTracker.RegisterError(chapterErrorName, stepName, obj);
        print($"[ErrorReporter] registrato errore per capitolo {chapterErrorName}");

        if(objectToFlash != null && executionOrderController != null)
        {
            executionOrderController.DifferentStepWarningHighlight(objectToFlash);
        }
    }
}
