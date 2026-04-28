using UnityEngine;
using System.Collections.Generic;

public class AttivatoreCasuale : MonoBehaviour
{
    [Header("Oggetti da gestire")]
    public GameObject[] oggetti; // Lista di oggetti da assegnare nell’Inspector

    [Header("Numero di oggetti da attivare")]
    [Range(0, 4)]
    public int quantitaDaAttivare = 1;

    void OnValidate()
    {
        // Se ci sono oggetti assegnati, imposta il limite massimo dinamicamente
        if (oggetti != null && oggetti.Length > 0)
        {
            quantitaDaAttivare = Mathf.Clamp(quantitaDaAttivare, 0, oggetti.Length);
        }
        else
        {
            quantitaDaAttivare = 0;
        }
    }

    void Start()
    {
      //AttivaOggettiCasuali();
    }

    [ContextMenu("Attiva Oggetti Casuali")]
    public void AttivaOggettiCasuali()
    {
        if (oggetti == null || oggetti.Length == 0)
        {
            Debug.LogWarning("Nessun oggetto assegnato!");
            return;
        }

        // Disattiva tutti gli oggetti
        foreach (var obj in oggetti)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        int n = Mathf.Min(quantitaDaAttivare, oggetti.Length);

        // Seleziona indici casuali unici
        HashSet<int> indici = new HashSet<int>();
        while (indici.Count < n)
        {
            int randomIndex = Random.Range(0, oggetti.Length);
            indici.Add(randomIndex);
        }

        // Attiva gli oggetti selezionati
        foreach (int i in indici)
        {
            if (oggetti[i] != null)
                oggetti[i].SetActive(true);
        }

        Debug.Log($"Attivati {n} oggetti casuali su {oggetti.Length} totali.");
    }
}
