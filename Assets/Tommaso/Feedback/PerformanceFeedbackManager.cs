using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class PerformanceManager : MonoBehaviour
{
    [System.Serializable]
    public class Chapter
    {
        public string chapterName;
        public List<string> ordineCorretto = new List<string> { "Step 1", "Step 2", "Step 3" };
        public List<string> ordineEseguito = new List<string>();

        public bool IsOrderCorrect()
        {
            if (ordineEseguito.Count != ordineCorretto.Count)
                return false;

            for (int i = 0; i < ordineCorretto.Count; i++)
                if (ordineEseguito[i] != ordineCorretto[i])
                    return false;

            return true;
        }
    }

    [Header("Lista dei capitoli")]
    public List<Chapter> capitoli = new List<Chapter>();

    [Header("Capitolo attivo per il test")]
    public int capitoloAttivo = 0;

    [Header("Prefab per il feedback di recap")]
    public GameObject FeedbackRecapprefab;

    private void Reset()
    {
        capitoli = new List<Chapter>
        {
            new Chapter { chapterName = "Capitolo 1" },
            new Chapter { chapterName = "Capitolo 2" },
            new Chapter { chapterName = "Capitolo 3" }
        };
    }

    /// <summary>
    /// Genera il testo di recap del capitolo attivo.
    /// </summary>
    public string GetChapterRecapText()
    {
        if (capitoloAttivo < 0 || capitoloAttivo >= capitoli.Count)
            return "Capitolo non valido.";

        Chapter c = capitoli[capitoloAttivo];
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"<b>{c.chapterName}</b>\n");
        sb.AppendLine("<size=26><b>Riepilogo ordine step:</b></size>\n");

        for (int i = 0; i < c.ordineCorretto.Count; i++)
        {
            string previsto = c.ordineCorretto[i];
            string eseguito = i < c.ordineEseguito.Count ? c.ordineEseguito[i] : "(mancante)";
            string icon = previsto == eseguito ? "✅" : "❌";
            sb.AppendLine($"{icon} Step {i + 1}: previsto <b>{previsto}</b>, eseguito <b>{eseguito}</b>");
        }

        sb.AppendLine();
        sb.AppendLine($"Risultato finale: {(c.IsOrderCorrect() ? "<color=green>Ordine corretto</color>" : "<color=red>Ordine errato</color>")}");

        return sb.ToString();
    }
}
