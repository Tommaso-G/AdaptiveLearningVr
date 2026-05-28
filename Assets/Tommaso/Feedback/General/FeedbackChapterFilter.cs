using UnityEngine;
using System.Collections.Generic;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using System.Linq;
using static ChapterTimer;

public class FeedbackChapterFilter : MonoBehaviour
{
    [System.Serializable]
    public class ChapterFeedbackSetting
    {
        [HideInInspector]
        public string chapterName;
        //  0 = feedback + outline normali
        //  1 = solo outline (no feedback)
        //  2 = niente (nessun aiuto)
        [Range(0, 2)]
        public int feedbackLevel;
        public bool outlineAlwaysVisibile = false;
    }


    public List<ChapterFeedbackSetting> chapterSettings = new List<ChapterFeedbackSetting>();

    private Dictionary<string, string> parentMap = new Dictionary<string, string>();

    public void SetFeedbackLevel(string chapterName, int level)
    {
        ChapterFeedbackSetting chapter = chapterSettings.FirstOrDefault(cf => cf.chapterName == chapterName);
        if (chapter == null)
        {
            Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato in chapterSettings.");
            return;
        }

        chapter.feedbackLevel = Mathf.Clamp(level, 0, 2);
    }

    /// <summary>
    /// Restituisce il livello di feedback corrente per il capitolo.
    /// Ritorna 0 se il capitolo non è registrato.
    /// </summary>
    public int GetFeedbackLevel(string chapterName)
    {
        ChapterFeedbackSetting chapter = chapterSettings.FirstOrDefault(cf => cf.chapterName == chapterName);
        if (chapter == null)
        {
            Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato in chapterSettings.");
            return 0;
        }
        return chapter.feedbackLevel;
    }

    public void NoneLevelChangeChapter(string chapterName)
    {
        ChapterFeedbackSetting chapter = chapterSettings.FirstOrDefault(cf => cf.chapterName == chapterName);
        if (chapter == null)
        {
            Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato in chapterSettings.");
            return;
        }
        chapter.feedbackLevel = 2;
    }
    private void Start()
    {
        if (ProcessRunner.Current != null)
            Initialize(ProcessRunner.Current);
        else
            ProcessRunner.Events.ProcessStarted += OnProcessStarted;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        Initialize(args.Process);
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
    }

    public void Initialize(IProcess process)
    {
        foreach (IChapter chapter in process.Data.Chapters)
            RegisterChapter(chapter, parentName: null);
    }

    private void RegisterChapter(IChapter chapter, string parentName)
    {
        if (chapter == null) return;

        string name = chapter.Data.Name;

        if (!chapterSettings.Exists(s => s.chapterName == name))
        {
            chapterSettings.Add(new ChapterFeedbackSetting
            {
                chapterName = name,
                feedbackLevel = 0
            });
        }

        print($"[FeedbackChapterFilter] capitolo {name} registrato. Parent name: {(parentName != null ? parentName : "assente")}");

        if (parentName != null && !parentMap.ContainsKey(name))
            parentMap[name] = parentName;

        if (chapter.Data?.Steps == null) return;

        foreach (var stepChild in chapter.Data.Steps)
        {
            if (stepChild is not IStep step) continue;

            foreach (var behavior in step.Data.Behaviors.Data.Behaviors)
            {
                if (behavior is not ExecuteChaptersBehavior exec) continue;

                foreach (var sub in exec.Data.SubChapters)
                {
                    if (sub?.Chapter == null) continue;
                    RegisterChapter(sub.Chapter, parentName: name);
                }
            }
        }
    }

    private int GetEffectiveFeedbackLevel(string chapterName)
    {
        if (!chapterName.Contains("Optional") && parentMap.TryGetValue(chapterName, out string parentName))
        {
            return GetEffectiveFeedbackLevel(parentName);
        }

        var setting = chapterSettings.Find(s => s.chapterName == chapterName);
        return setting?.feedbackLevel ?? 0;
    }

    public bool IsFeedbackAllowed(string chapterName)
    {
        var setting = chapterSettings.Find(s => s.chapterName == chapterName);
        if (setting == null)
        {
            Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato.");
            return true;
        }
        // Feedback visibile a livello -1 e 0
        return GetEffectiveFeedbackLevel(chapterName) == 0;
    }

    public bool IsOutlineAllowed(string chapterName)
    {
        var setting = chapterSettings.Find(s => s.chapterName == chapterName);
        if (setting == null)
        {
            Debug.LogWarning($"[FeedbackChapterFilter] Capitolo '{chapterName}' non trovato.");
            return true;
        }
        // Outline visibile a livello -1, 0 e 1
        return GetEffectiveFeedbackLevel(chapterName) <= 1;
    }

    public void SetOutlineAlwaysVisibile(string chapterName)
    {
        var setting = chapterSettings.Find(s => s.chapterName == chapterName);
        if (setting == null) return;
        setting.outlineAlwaysVisibile = true;
    }

    /// <summary>
    /// Restituisce true quando il livello è -1:
    /// OutlineManager deve passare a "Outline All" e i waypoint
    /// devono essere spostati sul layer Default.
    /// </summary>
    public bool IsHardAssistActive(string chapterName)
    {
        var setting = chapterSettings.Find(s => s.chapterName == chapterName);
        if (setting == null) return false;
        return setting.outlineAlwaysVisibile;
    }
}