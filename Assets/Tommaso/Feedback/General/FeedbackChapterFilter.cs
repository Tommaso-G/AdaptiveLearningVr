using UnityEngine;
using System.Collections.Generic;
using VRBuilder.Core;
using System.Linq;

public class FeedbackChapterFilter : MonoBehaviour
{
    [System.Serializable]
    public class ChapterFeedbackSetting
    {
        [HideInInspector]
        public string chapterName;
        [Range(0, 2)]
        public int feedbackLevel; // 0 o 1 = disattivato, 2 = attivato
    }

    public List<ChapterFeedbackSetting> chapterSettings = new List<ChapterFeedbackSetting>();

    public void setFeedbackLevel(string chapterName, int level)
    {
        ChapterFeedbackSetting chapter = chapterSettings.FirstOrDefault(cf => cf.chapterName == chapterName);
        chapter.feedbackLevel = level;
        print("Capitolo: " +  chapterName + " , feedback level: " +  chapter.feedbackLevel);
    }
    public void Initialize(IProcess process)
    {
        foreach (IChapter chapter in process.Data.Chapters)
        {
            string name = chapter.Data.Name;

            if (!chapterSettings.Exists(s => s.chapterName == name))
            {
                chapterSettings.Add(new ChapterFeedbackSetting
                {
                    chapterName = name,
                    feedbackLevel = 0
                });
            }
        }
    }

    public bool IsFeedbackAllowed(string chapterName)
        {
            var setting = chapterSettings.Find(s => s.chapterName == chapterName);
            if (setting == null)
            {
                Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato.");
                return true;
            }
            return setting.feedbackLevel == 0;
        }

    public bool IsOutlineAllowed(string chapterName)
        {
            var setting = chapterSettings.Find(s => s.chapterName == chapterName);
            if (setting == null)
            {
                Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato.");
                return true;
            }
            return setting.feedbackLevel <= 1;
        }
}