using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static FeedbackRepository;
public class MinigameDataRecorder : MonoBehaviour
{
    public Dictionary <string, MinigameDataContainer> minigameDataList = new Dictionary<string, MinigameDataContainer> ();
    public void RecordData(MinigameDataContainer container)
    {
        MinigameDataContainer data;

        if (string.IsNullOrEmpty(container.minigameChapterName))
        {
            print("Why, oh why, oh why-oh, why did I ever leave Ohio?");
            return;
        }

        if (minigameDataList.TryGetValue(container.minigameChapterName, out data))
        {
            data = container;
        }
        else
        {
            data = new MinigameDataContainer();
            data.minigameChapterName = container.minigameChapterName;
            data.completitionTime = container.completitionTime;
            data.errors = container.errors;
            data.moves = container.moves;
            minigameDataList.Add(container.minigameChapterName, data);
            Debug.Log("Dati del minigame: " + data.minigameChapterName + " salvati nel recorder");
        }
    }

    public void printRecorderSavings()
    {
        if (minigameDataList == null)
        {
            print("Minigame recorder vuoto");
            return;
        }

        foreach (var minigameData in minigameDataList)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("MINIGAME: " + minigameData.Value.minigameChapterName);

            sb.AppendLine("Tempo completamento: " + minigameData.Value.completitionTime);
            sb.AppendLine("Errori commessi: " + (minigameData.Value.errors == -1 ? "Non rilevanti per il minigame" : minigameData.Value.errors));
            sb.AppendLine("Mosse eseguite: " + (minigameData.Value.moves == -1 ? "Non rilevanti per il minigame" : minigameData.Value.moves));
            sb.AppendLine("----------------------");

            Debug.Log(sb.ToString());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            printRecorderSavings();
        }
    }
}
