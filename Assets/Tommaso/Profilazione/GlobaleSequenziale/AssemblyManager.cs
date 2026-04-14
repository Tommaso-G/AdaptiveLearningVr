using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyManager : MonoBehaviour, ICompletableStep
{
    [HideInInspector]
    public float PunteggioSequenziale { get; private set; }
    [HideInInspector]
    public float PunteggioGlobale { get; private set; }
    public bool IsCompleted { get; private set; } = false;

    private int _riaperturePannello = 0;
    private float _tempoInizio = -1f;
    private bool _timerAttivo = false;

    [Header("Contenitore principale degli oggetti")]
    public Transform piecesParent;

    [Header("Evento finale")]
    public UnityEvent OnAllCorrect;

    private readonly Dictionary<CorrectGridCell, bool> pieceStates = new();

    [Header("Sequenze ideali per FSLSM")]
    public List<int> sequenzaIdealeSequenziale = new List<int>();
    public List<int> sequenzaIdealeGlobale = new List<int>();

    private readonly List<int> _sequenzaUtente = new List<int>();

    [Header("Area di attivazione")]
    [Tooltip("Collider che definisce la zona in cui il pannello rimane visibile")]
    public Collider zonaAttivazione;
    [Tooltip("Transform del player da controllare")]
    public Transform player;
    [Tooltip("Pannello da mostrare/nascondere")]
    public GameObject pannello;

    private bool hasStarted = false;

    private void Awake()
    {
        if (piecesParent == null)
        {
            Debug.LogError("AssemblyManager: nessun 'piecesParent' assegnato!");
            return;
        }

        var pieces = piecesParent.GetComponentsInChildren<CorrectGridCell>(includeInactive: true);

        if (pieces.Length == 0)
        {
            Debug.LogWarning($"AssemblyManager: nessun CorrectGridCell trovato sotto '{piecesParent.name}'");
            return;
        }

        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            pieceStates[piece] = false;
            piece.OnCheckSnap.AddListener((isCorrect) => OnPieceStateChanged(piece, isCorrect));
        }

        Debug.Log($"AssemblyManager: trovati {pieces.Length} pezzi sotto '{piecesParent.name}'.");
    }

    private void Update()
    {
        if (!hasStarted) return;
        if (player == null || zonaAttivazione == null || pannello == null) return;

        bool playerNellaZona = zonaAttivazione.bounds.Contains(player.position);
        if (!playerNellaZona && pannello.activeSelf)
        {
            pannello.SetActive(false);
        }
    }

    /// <summary>
    /// Da collegare al pulsante.
    /// - Prima volta → avvia il gioco e mostra il pannello.
    /// - Volte successive → riattiva il pannello se il player è nella zona.
    /// - Pannello già attivo → non fa nulla.
    /// </summary>
    public void AttivaOAvviaGioco()
    {
        if (player != null && zonaAttivazione != null)
        {
            bool playerNellaZona = zonaAttivazione.bounds.Contains(player.position);
            if (!playerNellaZona)
            {
                Debug.Log("Il player non è nella zona di attivazione.");
                return;
            }
        }

        if (pannello != null && pannello.activeSelf)
        {
            Debug.Log("Pannello già attivo, nessuna azione.");
            return;
        }

        if (!hasStarted)
        {
            hasStarted = true;
            _tempoInizio = Time.time;
            _timerAttivo = true;
            Debug.Log("Gioco avviato per la prima volta.");
        }
        else
        {
            _riaperturePannello++;
            Debug.Log($"Pannello riattivato. Riaperture: {_riaperturePannello}");
        }

        if (pannello != null)
            pannello.SetActive(true);
    }

    private void OnDestroy()
    {
        foreach (var kvp in pieceStates)
            kvp.Key.OnCheckSnap.RemoveAllListeners();
    }

    private void OnPieceStateChanged(CorrectGridCell piece, bool isCorrect)
    {
        if (!pieceStates.ContainsKey(piece)) return;

        bool eraCorretto = pieceStates[piece];
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
            PunteggioSequenziale = risultato.punteggioSequenziale;
            PunteggioGlobale = risultato.punteggioGlobale;

            Debug.Log($"Stile FSLSM → Sequenziale: {risultato.punteggioSequenziale:F2} | Globale: {risultato.punteggioGlobale:F2}");
            OnAllCorrect?.Invoke();
            IsCompleted = true;
        }
        else
        {
            PrintMissingPiecesIfFew();
        }
    }

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
            if (!state) return false;

        return true;
    }

    public float GetTempoTotale()
    {
        if (_tempoInizio < 0) return 0f;
        return Time.time - _tempoInizio;
    }

    public int GetRiaperturePannello() => _riaperturePannello;
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