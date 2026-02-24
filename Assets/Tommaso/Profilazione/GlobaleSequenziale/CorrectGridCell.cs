using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[System.Serializable]
public class GridSnapEvent : UnityEvent<bool> { }

[RequireComponent(typeof(XRGrabInteractable))]
public class CorrectGridCell : MonoBehaviour
{
    [Header("Griglia e cella corretta")]
    public XRGridSocketInteractor gridSocket;  // Riferimento alla griglia (opzionale)
    public int correctRow = 0;
    public int correctColumn = 0;

    [Header("Socket singolo (se presente, ignora la griglia)")]
    public XRSocketInteractor correctSingleSocket;  // 🔹 Socket singolo assegnabile

    [Header("Evento")]
    public GridSnapEvent OnCheckSnap;  // true se l'oggetto è posizionato correttamente

    private XRGrabInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();

        if (gridSocket == null && correctSingleSocket == null)
            Debug.LogWarning($"{name}: nessun XRGridSocketInteractor o socket singolo assegnato — non potrà essere controllato.");

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDestroy()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        CheckSnap(args.interactorObject);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        CheckSnap(args.interactorObject);
    }

    private void CheckSnap(IXRSelectInteractor interactor)
    {
        if (interactor == null)
            return;

        bool correct = false;

        // Caso A → è stato assegnato un socket singolo
        if (correctSingleSocket != null)
        {
            if (interactor is XRSocketInteractor singleSocket && singleSocket == correctSingleSocket)
            {
                correct = true;
                Debug.Log($"{name} è attaccato al socket singolo corretto ({singleSocket.transform.name}).");
            }
            else
            {
                correct = false;
                Debug.Log($"{name} NON è attaccato al socket corretto (attuale: {interactor.transform.name}).");
            }
        }
        // Caso B → nessun socket singolo assegnato → usa la griglia
        else if (gridSocket != null && interactor is XRGridSocketInteractor)
        {
            var pos = GetGridPosition(gridSocket);
            correct = pos.HasValue &&
                      pos.Value.row == correctRow &&
                      pos.Value.column == correctColumn;

            if (pos.HasValue)
                Debug.Log($"{name} è attaccato alla cella ({pos.Value.row},{pos.Value.column}) — {(correct ? "corretto ✅" : "sbagliato ❌")}");
            else
                Debug.Log($"{name} non risulta attaccato a nessuna cella nella griglia.");
        }
        else
        {
            Debug.Log($"{name}: interactor non riconosciuto ({interactor.GetType().Name}).");
        }

        OnCheckSnap?.Invoke(correct);
    }

    // 🔍 Restituisce la posizione (riga, colonna) dell'interactable nella griglia
    private (int row, int column)? GetGridPosition(XRGridSocketInteractor grid)
    {
        var dictField = typeof(XRGridSocketInteractor).GetField(
            "m_UsedAttachTransformByInteractable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var dict = dictField?.GetValue(grid) as System.Collections.Generic.Dictionary<IXRInteractable, Transform>;
        if (dict == null || !dict.ContainsKey(interactable))
            return null;

        Transform attachPoint = dict[interactable];

        var gridField = typeof(XRGridSocketInteractor).GetField(
            "m_Grid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var gridArray = gridField?.GetValue(grid) as Transform[,];
        if (gridArray == null)
            return null;

        int rows = gridArray.GetLength(0);
        int cols = gridArray.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (gridArray[i, j] == attachPoint)
                    return (i, j);
            }
        }

        return null;
    }
}

