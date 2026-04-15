using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VRBuilder.Core;
using static FeedbackChapterFilter;

public class DifficultyChapterFilter : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyChapterSetting
    {
        [HideInInspector]
        public string chapterName;
        [Range(0, 1)]
        public int difficultyLevel; // 0=base, 1=avanzato
        public UnityEvent OnAdvancedActivated;
        public UnityEvent OnBaseActivated;
    }

    public List<DifficultyChapterSetting> chapterSettings = new List<DifficultyChapterSetting>();

    public void Initialize(IProcess process)
    {
        foreach (IChapter chapter in process.Data.Chapters)
        {
            string name = chapter.Data.Name;

            if (!chapterSettings.Exists(s => s.chapterName == name))
            {
                chapterSettings.Add(new DifficultyChapterSetting
                {
                    chapterName = name,
                    difficultyLevel = 0
                });
            }
        }
    }

    public void SetDifficultyLevel(string chapterName, int level)
    {
        var chapter = chapterSettings
            .FirstOrDefault(c => c.chapterName == chapterName);

        if (chapter == null)
        {
            Debug.LogWarning($"Capitolo {chapterName} non trovato.");
            return;
        }

        chapter.difficultyLevel = level;

        if (level == 1)
            chapter.OnAdvancedActivated?.Invoke();
        else
            chapter.OnBaseActivated?.Invoke();
    }
}
