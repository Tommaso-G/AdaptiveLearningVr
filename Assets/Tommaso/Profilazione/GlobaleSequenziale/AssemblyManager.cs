using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyManager : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Header("Contenitore principale degli oggetti")]
    public Transform piecesParent; // 🔹 Oggetto padre che contiene tutti i pezzi da monitorare

    [Header("Evento finale")]
    public UnityEvent OnAllCorrect; // 🔹 Viene chiamato quando tutto è completato

    private readonly Dictionary<CorrectGridCell, bool> pieceStates = new();

    private void Awake()
    {
        if (piecesParent == null)
        {
            Debug.LogError("AssemblyManager: nessun 'piecesParent' assegnato!");
            return;
        }

        // Trova automaticamente tutti i CorrectGridCell nei figli del contenitore
        var pieces = piecesParent.GetComponentsInChildren<CorrectGridCell>(includeInactive: true);

        if (pieces.Length == 0)
        {
            Debug.LogWarning($"AssemblyManager: nessun CorrectGridCell trovato sotto '{piecesParent.name}'");
            return;
        }

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            // Inizializza stato
            pieceStates[piece] = false;

            // Iscrizione all'evento del pezzo
            piece.OnCheckSnap.AddListener((isCorrect) => OnPieceStateChanged(piece, isCorrect));
        }

        Debug.Log($"AssemblyManager: trovati {pieces.Length} pezzi sotto '{piecesParent.name}'.");
    }

    private void OnDestroy()
    {
        // Pulizia dei listener
        foreach (var kvp in pieceStates)
        {
            kvp.Key.OnCheckSnap.RemoveAllListeners();
        }
    }

    private void OnPieceStateChanged(CorrectGridCell piece, bool isCorrect)
    {
        if (!pieceStates.ContainsKey(piece))
            return;

        pieceStates[piece] = isCorrect;

        Debug.Log($"AssemblyManager: {piece.name} -> {(isCorrect ? "CORRETTO" : "SBAGLIATO")}");

        if (AllPiecesCorrect())
        {
            Debug.Log("✅ Tutti i pezzi sono al posto giusto! Puzzle completato!");
            IsCompleted= true;
            OnAllCorrect?.Invoke();
        }
        else
        {
            PrintMissingPiecesIfFew();
        }
    }

    // 🔹 Se mancano meno di 4 pezzi, li stampa
    private void PrintMissingPiecesIfFew()
    {
        var missing = pieceStates.Where(kvp => kvp.Value == false)
                                 .Select(kvp => kvp.Key.name)
                                 .ToList();

        int missingCount = missing.Count;

        if (missingCount > 0 && missingCount < 4)
        {
            string list = string.Join(", ", missing);
            Debug.Log($"⚠️ Mancano ancora {missingCount} pezzi: {list}");
        }
    }

    private bool AllPiecesCorrect()
    {
        foreach (var state in pieceStates.Values)
        {
            if (!state)
                return false;
        }
        return true;
    }
}