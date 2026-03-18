using System.Collections.Generic;
using UnityEngine;

public class VisivoVerbaleStatManager : MonoBehaviour
{
    [Header("Giochi da tracciare")]
    public List<MonoBehaviour> giochi = new List<MonoBehaviour>();

    // Struttura interna per accumulare i dati per modalità
    private class StatModalita
    {
        public float tempoTotale = 0f;
        public int erroriTotali = 0;
    }

    // gameID → modalità → stats accumulate
    private Dictionary<string, Dictionary<ModalitaGioco, StatModalita>> storico
        = new Dictionary<string, Dictionary<ModalitaGioco, StatModalita>>();

    private void Start()
    {
        foreach (var mb in giochi)
        {
            if (mb is ITrackableGameVisivoVerbale gioco)
            {
                gioco.OnRoundFinished += OnRoundRicevuto;
                Debug.Log($"GameStatsManager: registrato {gioco.GameID}");
            }
            else
            {
                Debug.LogWarning($"{mb.name} non implementa ITrackableGameVisivoVerbale");
            }
        }
    }

    private void OnRoundRicevuto(RoundData data)
    {
        // Crea la voce per il gameID se non esiste
        if (!storico.ContainsKey(data.gameID))
            storico[data.gameID] = new Dictionary<ModalitaGioco, StatModalita>();

        // Crea la voce per la modalità se non esiste
        if (!storico[data.gameID].ContainsKey(data.modalita))
            storico[data.gameID][data.modalita] = new StatModalita();

        // Accumula
        storico[data.gameID][data.modalita].tempoTotale += data.tempoSecondi;
        storico[data.gameID][data.modalita].erroriTotali += data.errori;
    }

    // Chiama questo metodo quando vuoi stampare il riepilogo
    public void StampaRiepilogo()
    {
        foreach (var gioco in storico)
        {
            Debug.Log($"── {gioco.Key} ──");
            foreach (var modalita in gioco.Value)
            {
                Debug.Log($"  {modalita.Key}: " +
                          $"tempo totale {modalita.Value.tempoTotale:F1}s, " +
                          $"errori totali {modalita.Value.erroriTotali}");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var mb in giochi)
        {
            if (mb is ITrackableGameVisivoVerbale gioco)
                gioco.OnRoundFinished -= OnRoundRicevuto;
        }
    }
}