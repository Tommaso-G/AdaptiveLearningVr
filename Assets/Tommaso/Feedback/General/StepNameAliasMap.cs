using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mappa i nomi tecnici degli step VRBuilder a stringhe human-readable.
/// Popola la tabella tramite il bottone "Carica step" nell'Inspector (Editor only).
/// </summary>
public class StepNameAliasMap : MonoBehaviour
{
    [System.Serializable]
    public class StepAlias
    {
        [HideInInspector]
        public string stepName;

        [Tooltip("Nome da mostrare nel log degli errori. Se vuoto, viene usato stepName.")]
        public string displayName;

        [Tooltip("Peso dell'errore associato a questo step mancato.")]
        public int weight = 1;
    }

    [System.Serializable]
    public class ChapterStepAlias
    {
        [Tooltip("Nome del capitolo VRBuilder.")]
        public string chapterName;

        public List<StepAlias> steps = new List<StepAlias>();
    }

    [Header("Tabella alias step")]
    public List<ChapterStepAlias> chapterStepAliases = new List<ChapterStepAlias>();

    // ─────────────────────────────────────────────────────────────────
    // LOOKUP RUNTIME
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Restituisce il displayName per lo step indicato nel capitolo indicato.
    /// Se non trova un alias (o il displayName è vuoto), restituisce stepName invariato.
    /// </summary>
    public string Resolve(string chapterName, string stepName)
    {
        foreach (var chapter in chapterStepAliases)
        {
            if (!string.Equals(chapter.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var alias in chapter.steps)
            {
                if (string.Equals(alias.stepName, stepName, System.StringComparison.OrdinalIgnoreCase))
                    return string.IsNullOrWhiteSpace(alias.displayName) ? stepName : alias.displayName;
            }

            // Capitolo trovato ma step non in lista → restituisce stepName
            return stepName;
        }

        // Capitolo non trovato → restituisce stepName
        return stepName;
    }

    public int ResolveWeight(string chapterName, string stepName, int defaultWeight = 1)
    {
        foreach (var chapter in chapterStepAliases)
        {
            if (!string.Equals(chapter.chapterName, chapterName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var alias in chapter.steps)
            {
                if (string.Equals(alias.stepName, stepName, System.StringComparison.OrdinalIgnoreCase))
                    return alias.weight;
            }

            return defaultWeight;
        }

        return defaultWeight;
    }
}