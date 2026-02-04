using System.Collections.Generic;
using UnityEngine;

public class GenericSequenceManager : MonoBehaviour
{
    [Tooltip("Lista di oggetti da completare (verrà mescolata casualmente all'avvio)")]
    public List<GameObject> sequenza = new List<GameObject>();

    private int indiceCorrente = 0;

    private void Start()
    {
        if (sequenza.Count == 0)
        {
            Debug.LogWarning("⚠️ Nessun oggetto assegnato alla sequenza!");
            return;
        }

        MischiaEAvvia();
    }

    public void ResetSequence()
    {
        if (sequenza.Count == 0)
        {
            Debug.LogWarning("⚠️ Nessun oggetto nella sequenza da resettare!");
            return;
        }

        Debug.Log("🔁 Reset della sequenza in corso...");
        MischiaEAvvia();
    }

    private void MischiaEAvvia()
    {
        indiceCorrente = 0;

        // Mischia la sequenza casualmente
        Shuffle(sequenza);

        // Stampa l'ordine scelto
        Debug.Log("🔀 Nuova sequenza mescolata:");
        for (int i = 0; i < sequenza.Count; i++)
        {
            Debug.Log($"Step {i + 1}: {sequenza[i].name}");
        }

        Debug.Log($"➡️ Inizia con: {sequenza[0].name}");
    }

    public void StepCompletato(GameObject oggetto)
    {
        if (indiceCorrente >= sequenza.Count)
            return;

        GameObject stepCorrente = sequenza[indiceCorrente];

        if (oggetto == stepCorrente)
        {
            Debug.Log($"✅ Step completato: {oggetto.name}");
            indiceCorrente++;

            if (indiceCorrente < sequenza.Count)
            {
                Debug.Log($"➡️ Prossimo step: {sequenza[indiceCorrente].name}");
            }
            else
            {
                Debug.Log("🎉 Tutti gli step completati!");
            }
        }
        else
        {
            Debug.Log($"❌ Hai attivato {oggetto.name}, ma ora serve {stepCorrente.name}");
        }
    }

    private void Shuffle<T>(IList<T> lista)
    {
        System.Random rng = new System.Random();
        int n = lista.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (lista[k], lista[n]) = (lista[n], lista[k]);
        }
    }

    private void ShowSequence()
    {
        
    }
}
