using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VRBuilder.Core;

public class ChapterSkipHandler : MonoBehaviour
{
    [System.Serializable]
    public class ChapterSkipSetting
    {
        public string chapterName;
        public UnityEvent onChapterSkipped;
    }

    public List<ChapterSkipSetting> chapterSettings = new List<ChapterSkipSetting>();

    public void Initialize(IProcess process)
    {
        foreach (IChapter chapter in process.Data.Chapters)
        {
            string name = chapter.Data.Name;

            if (!chapterSettings.Exists(s => s.chapterName == name))
            {
                chapterSettings.Add(new ChapterSkipSetting
                {
                    chapterName = name
                });
            }
        }
    }

    public void NotifyChapterSkipped(string chapterName)
    {
        var chapter = chapterSettings.FirstOrDefault(c => c.chapterName == chapterName);

        if (chapter == null)
        {
            Debug.LogWarning($"[ChapterSkipHandler] Capitolo {chapterName} non trovato.");
            return;
        }

        Debug.Log($"[ChapterSkipHandler] Capitolo saltato: {chapterName}");

        chapter.onChapterSkipped?.Invoke();
    }
}
