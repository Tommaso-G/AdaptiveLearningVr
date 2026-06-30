using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class FeedbackRepositoryExporter
{
    [MenuItem("Tools/Export Feedback Hierarchy JSON")]
    public static void Export()
    {
        string[] guids = AssetDatabase.FindAssets("t:FeedbackRepository");

        if (guids.Length == 0)
        {
            Debug.LogError("FeedbackRepository non trovato.");
            return;
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        FeedbackRepository repo = AssetDatabase.LoadAssetAtPath<FeedbackRepository>(assetPath);

        Root root = new Root
        {
            Global = ExportPath(repo.globalPath),
            Sequential = ExportPath(repo.sequentialPath)
        };

        if (repo.exceptionFeedbacks != null && repo.exceptionFeedbacks.Count > 0)
        {
            root.ExceptionGlobal = ExportPath(repo.exceptionFeedbacks[0].globalPath);
            root.ExceptionSequential = ExportPath(repo.exceptionFeedbacks[0].sequentialPath);
        }

        string json = JsonUtility.ToJson(root, true);

        string outputPath = Path.Combine(Application.dataPath, "feedback_hierarchy.json");
        File.WriteAllText(outputPath, json);

        AssetDatabase.Refresh();

        Debug.Log("JSON esportato in:\n" + outputPath);
    }

    // ===================== EXPORT CORE =====================

    static PathNode ExportPath(FeedbackRepository.PathGroup path)
    {
        return new PathNode
        {
            Visual = ExportMode(path.visualPath.attivo, path.visualPath.riflessivo),
            Verbal = ExportMode(path.verbalPath.attivo, path.verbalPath.riflessivo)
        };
    }

    static ModeNode ExportMode(List<FeedbackRepository.Chapter> active,
                               List<FeedbackRepository.Chapter> reflective)
    {
        return new ModeNode
        {
            Active = ExportChapters(active),
            Reflective = ExportChapters(reflective)
        };
    }

    static List<ChapterNode> ExportChapters(List<FeedbackRepository.Chapter> chapters)
    {
        var result = new List<ChapterNode>();

        if (chapters == null) return result;

        foreach (var chapter in chapters)
        {
            var feedbackNames = new List<string>();

            if (chapter.feedbacks != null)
            {
                foreach (var f in chapter.feedbacks)
                {
                    if (!string.IsNullOrEmpty(f.FeedbackName))
                        feedbackNames.Add(f.FeedbackName);
                }
            }

            result.Add(new ChapterNode
            {
                ChapterName = chapter.ChapterName,
                Feedbacks = feedbackNames
            });
        }

        return result;
    }

    // ===================== JSON STRUCTURE =====================

    [System.Serializable]
    class Root
    {
        public PathNode Global;
        public PathNode Sequential;
        public PathNode ExceptionGlobal;
        public PathNode ExceptionSequential;
    }

    [System.Serializable]
    class PathNode
    {
        public ModeNode Visual;
        public ModeNode Verbal;
    }

    [System.Serializable]
    class ModeNode
    {
        public List<ChapterNode> Active;
        public List<ChapterNode> Reflective;
    }

    [System.Serializable]
    class ChapterNode
    {
        public string ChapterName;
        public List<string> Feedbacks;
    }
}