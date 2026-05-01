using UnityEngine;
using System.Collections.Generic;

public class AttivatoreCasuale : MonoBehaviour
{
    [Header("Oggetti da gestire")]
    public GameObject[] oggetti;

    [Header("Numero di oggetti da attivare")]
    [Range(0, 4)]
    public int quantitaDaAttivare = 1;

    public bool IsActiveCondition = false;

    void OnValidate()
    {
        if (oggetti != null && oggetti.Length > 0)
        {
            quantitaDaAttivare = Mathf.Clamp(quantitaDaAttivare, 0, oggetti.Length);
        }
        else
        {
            quantitaDaAttivare = 0;
        }
    }

    [ContextMenu("Attiva Oggetti Casuali")]
    public void AttivaOggettiCasuali()
    {

        if (!IsActiveCondition)
        {
            return;
        }

        if (oggetti == null || oggetti.Length == 0)
        {
            Debug.LogWarning("Nessun oggetto assegnato!");
            return;
        }

        foreach (var obj in oggetti)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        int n = Mathf.Min(quantitaDaAttivare, oggetti.Length);

        HashSet<int> indici = new HashSet<int>();
        while (indici.Count < n)
        {
            int randomIndex = Random.Range(0, oggetti.Length);
            indici.Add(randomIndex);
        }

        foreach (int i in indici)
        {
            if (oggetti[i] != null)
                oggetti[i].SetActive(true);
        }

        Debug.Log($"Attivati {n} oggetti casuali su {oggetti.Length} totali.");
    }

    public void SetIsActiveConditionTrue()
    {
        IsActiveCondition = true;
    }

    public void SetIsActiveConditionFalse()
    {
        IsActiveCondition = false;
    }
}