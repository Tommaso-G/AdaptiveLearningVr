using System.Collections.Generic;
using UnityEngine;

public class VisivoVerbaleStatManager : MonoBehaviour
{
    [Header("Giochi da tracciare")]
    public List<MonoBehaviour> giochi = new List<MonoBehaviour>();

    // Struttura interna per accumulare i dati per modalità
    public class StatModalita
    {
        public float tempoTotale = 0f;
        public int erroriTotali = 0;
        public Dictionary<string, float> parametriExtra = new Dictionary<string, float>();

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
        if (!storico.ContainsKey(data.gameID))
            storico[data.gameID] = new Dictionary<ModalitaGioco, StatModalita>();

        if (!storico[data.gameID].ContainsKey(data.modalita))
            storico[data.gameID][data.modalita] = new StatModalita();

        storico[data.gameID][data.modalita].tempoTotale += data.tempoSecondi;
        storico[data.gameID][data.modalita].erroriTotali += data.errori;

        // ← null check prima di iterare
        if (data.parametriExtra != null)
        {
            var stat = storico[data.gameID][data.modalita];
            foreach (var kv in data.parametriExtra)
            {
                if (!stat.parametriExtra.ContainsKey(kv.Key))
                    stat.parametriExtra[kv.Key] = 0f;
                stat.parametriExtra[kv.Key] += kv.Value;
            }
        }
    }

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

                // ← stampa parametri extra se presenti
                foreach (var kv in modalita.Value.parametriExtra)
                    Debug.Log($"    {kv.Key}: {kv.Value:F1}s");
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

    void Update()
    {   
        if(Input.GetKeyDown(KeyCode.B))
        {
            StampaRiepilogo();
        }
    }

    public Dictionary<string, Dictionary<ModalitaGioco, StatModalita>> GetStorico()
    {
        return storico;
    }
}