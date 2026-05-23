using UnityEngine;

/// <summary>
/// Disabilita tutti i GameObject della lista e ne abilita uno a caso.
/// Chiama PickRandom() da codice o dal Inspector tramite il bottone di contesto.
/// </summary>
public class RandomGameObjectPicker : MonoBehaviour
{
    [Tooltip("Lista di GameObject tra cui scegliere casualmente.")]
    public GameObject[] candidates;

    [Tooltip("Se true, esegue la selezione automaticamente all'avvio.")]
    public bool pickOnStart = true;

    [Tooltip("Indice dell'ultimo GameObject abilitato (-1 = nessuno).")]
    [HideInInspector]
    public int lastPickedIndex = -1;

    // -------------------------------------------------------

    void Start()
    {
        if (pickOnStart)
            PickRandom();
    }

    /// <summary>
    /// Disabilita tutti i candidati e ne abilita uno a caso.
    /// Può essere chiamato da altri script o da UnityEvent.
    /// </summary>
    public void PickRandom()
    {
        if (candidates == null || candidates.Length == 0)
        {
            Debug.LogWarning("[RandomGameObjectPicker] La lista 'candidates' è vuota!");
            return;
        }

        // Disabilita tutti
        foreach (var go in candidates)
        {
            if (go != null)
                go.SetActive(false);
        }

        // Sceglie un indice a caso
        int index = Random.Range(0, candidates.Length);

        // Abilita il prescelto
        if (candidates[index] != null)
        {
            candidates[index].SetActive(true);
            lastPickedIndex = index;
            Debug.Log($"[RandomGameObjectPicker] Abilitato: {candidates[index].name} (indice {index})");
        }
        else
        {
            Debug.LogWarning($"[RandomGameObjectPicker] Il candidato all'indice {index} è null!");
        }
    }

    /// <summary>
    /// Disabilita tutti i candidati senza abilitarne nessuno.
    /// </summary>
    public void DisableAll()
    {
        if (candidates == null) return;

        foreach (var go in candidates)
        {
            if (go != null)
                go.SetActive(false);
        }

        lastPickedIndex = -1;
    }
}