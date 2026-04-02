using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyManager : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Header("Contenitore principale degli oggetti")]
    public Transform piecesParent; //Oggetto padre che contiene tutti i pezzi da monitorare

    [Header("Evento finale")]
    public UnityEvent OnAllCorrect; //Viene chiamato quando tutto è completato

    private readonly Dictionary<CorrectGridCell, bool> pieceStates = new();

    [Header("Sequenze ideali per FSLSM")]
    public List<int> sequenzaIdealeSequenziale = new List<int>();
    public List<int> sequenzaIdealeGlobale = new List<int>();

    private readonly List<int> _sequenzaUtente = new List<int>();

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

        bool eraCorretto = pieceStates[piece]; // salva PRIMA di aggiornare
        pieceStates[piece] = isCorrect;

        Debug.Log($"AssemblyManager: {piece.name} -> {(isCorrect ? "CORRETTO" : "SBAGLIATO")}");


        if (isCorrect && !eraCorretto && !_sequenzaUtente.Contains(piece.pezzoID))
            _sequenzaUtente.Add(piece.pezzoID);

        if (AllPiecesCorrect())
        {
            Debug.Log("Tutti i pezzi sono al posto giusto! Puzzle completato!");

            var risultato = SequenceAnalyzer.CalcolaStile(
                _sequenzaUtente,
                sequenzaIdealeSequenziale,
                sequenzaIdealeGlobale
            );

            Debug.Log($"Stile FSLSM → Sequenziale: {risultato.punteggioSequenziale:F2} | Globale: {risultato.punteggioGlobale:F2}");
                OnAllCorrect?.Invoke();

            IsCompleted= true;
        }
        else
        {
            PrintMissingPiecesIfFew();
        }
    }

    //Se mancano meno di 4 pezzi, li stampa
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


    public static class SequenceAnalyzer
    {
        public struct RisultatoStile
        {
            public float punteggioSequenziale;
            public float punteggioGlobale;
        }

        public static RisultatoStile CalcolaStile(
            List<int> sequenzaUtente,
            List<int> sequenzaIdealeSequenziale,
            List<int> sequenzaIdealeGlobale)
        {
            float punteggioSequenziale = Spearman(sequenzaUtente, sequenzaIdealeSequenziale);
            float punteggioGlobale = Spearman(sequenzaUtente, sequenzaIdealeGlobale);

            return new RisultatoStile
            {
                punteggioSequenziale = punteggioSequenziale,
                punteggioGlobale     = punteggioGlobale
            };
        }

        private static float Spearman(List<int> a, List<int> b)
        {
            int n = a.Count;
            if (n <= 1) return 1f;

            float[] ranghiA = CalcolaRanghi(a);
            float[] ranghiB = CalcolaRanghi(b);

            float sommaDiffQuadri = 0f;
            for (int i = 0; i < n; i++)
            {
                float d = ranghiA[i] - ranghiB[i];
                sommaDiffQuadri += d * d;
            }

            float spearman = 1f - (6f * sommaDiffQuadri) / (n * (n * n - 1));
            return (spearman + 1f) / 2f;
        }

        private static float[] CalcolaRanghi(List<int> valori)
        {
            int n = valori.Count;
            float[] ranghi = new float[n];

            for (int i = 0; i < n; i++)
            {
                int rango = 1;
                for (int j = 0; j < n; j++)
                    if (valori[j] < valori[i]) rango++;
                ranghi[i] = rango;
            }

            return ranghi;
        }
    }