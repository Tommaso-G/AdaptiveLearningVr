using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

public static class VRBuilderJsonReader
{
    public static (List<string> chapters, List<string> steps) ParseProcessJson(string processJsonPath)
    {
        if (!File.Exists(processJsonPath))
        {
            Debug.LogError($"[VRBuilderJsonReader] File non trovato: {processJsonPath}");
            return (new(), new());
        }

        string json = File.ReadAllText(processJsonPath);
        JObject root = JObject.Parse(json);

        Dictionary<string, JToken> idMap = new();
        BuildIdMap(root, idMap);

        var guidToName = BuildGuidToNameMap(root);

        JToken chaptersToken = root.SelectToken("Process.Data.Chapters.$values");
        if (chaptersToken == null)
        {
            Debug.LogWarning("[VRBuilderJsonReader] Nessun nodo Chapters trovato nel file.");
            return (new(), new());
        }

        List<string> chapters = new();
        List<string> steps = new();

        foreach (var chapterRef in chaptersToken)
        {
            JToken chapter = ResolveRef(chapterRef, idMap);
            if (chapter != null)
                CollectChapterRecursive(chapter, chapters, steps, idMap, guidToName);
        }

        //Debug.Log($"[VRBuilderJsonReader] Trovati {chapters.Count} capitoli e {steps.Count} step (inclusi subchapter).");
        return (chapters, steps);
    }

private static void CollectChapterRecursive(
    JToken chapter,
    List<string> chapters,
    List<string> steps,
    Dictionary<string, JToken> idMap,
    Dictionary<string, string> guidToName)
{
    string chapterName = chapter["Data"]?["Name"]?.ToString();
    if (string.IsNullOrEmpty(chapterName))
        return;

    chapters.Add(chapterName);
    steps.Add($"--- {chapterName.ToUpper()} ---");

    var stepsToken = chapter.SelectToken("Data.Steps.$values");
    if (stepsToken != null)
    {
        foreach (var stepRef in stepsToken)
        {
            JToken step = ResolveRef(stepRef, idMap);
            if (step == null) continue;

            // Prima inseriamo eventuali subchapter
            var behaviorsToken = step.SelectToken("Data.Behaviors.Data.Behaviors.$values");
            if (behaviorsToken != null)
            {
                foreach (var behavior in behaviorsToken)
                {
                    string type = behavior["$type"]?.ToString() ?? "";
                    if (type.Contains("ExecuteChaptersBehavior"))
                    {
                        var subChaptersToken = behavior.SelectToken("Data.SubChapters.$values");
                        if (subChaptersToken != null)
                        {
                            foreach (var sub in subChaptersToken)
                            {
                                JToken subChapter = ResolveRef(sub["Chapter"], idMap);
                                if (subChapter != null)
                                {
                                    CollectSubChapterRecursive(subChapter, chapters, steps, idMap, guidToName, 1);
                                }
                            }
                        }
                    }
                }
            }

            // Poi inseriamo lo step principale
            string guid = step.SelectToken("StepMetadata.Guid")?.ToString();
            if (!string.IsNullOrEmpty(guid) && guidToName.TryGetValue(guid, out string name))
                steps.Add(name);
            else if (!string.IsNullOrEmpty(guid))
                steps.Add($"Step {guid.Substring(0, 8)}");
        }
    }
}

private static void CollectSubChapterRecursive(
    JToken chapter,
    List<string> chapters,
    List<string> steps,
    Dictionary<string, JToken> idMap,
    Dictionary<string, string> guidToName,
    int indentLevel)
{
    // 1. Nome del capitolo corrente
    string chapterName = chapter["Data"]?["Name"]?.ToString();
    if (string.IsNullOrEmpty(chapterName))
        return;

    chapters.Add(chapterName);
    steps.Add($"{new string(' ', indentLevel * 4)}--- {chapterName.ToUpper()} ---");

    // 2. Recupera gli step di questo capitolo
    var stepsToken = chapter.SelectToken("Data.Steps.$values");
    if (stepsToken == null)
        return;

    // 3. Scorri tutti gli step del capitolo nell’ordine originale
    foreach (var stepRef in stepsToken)
    {
        JToken step = ResolveRef(stepRef, idMap);
        if (step == null)
            continue;

        // === A. Nome dello step ===
        string guid = step.SelectToken("StepMetadata.Guid")?.ToString();
        string stepName = null;
        if (!string.IsNullOrEmpty(guid))
        {
            if (guidToName.TryGetValue(guid, out string resolvedName))
                stepName = resolvedName;
            else
                stepName = $"Step {guid.Substring(0, 8)}";
        }
        else
        {
            stepName = "(Step senza GUID)";
        }

        steps.Add(stepName);

        // === B. Dopo aver aggiunto lo step, controlla se ha subchapter ===
        var behaviorsToken = step.SelectToken("Data.Behaviors.Data.Behaviors.$values");
        if (behaviorsToken == null)
            continue;

        foreach (var behavior in behaviorsToken)
        {
            string type = behavior["$type"]?.ToString() ?? "";
            if (!type.Contains("ExecuteChaptersBehavior"))
                continue;

            var subChaptersToken = behavior.SelectToken("Data.SubChapters.$values");
            if (subChaptersToken == null)
                continue;

            foreach (var sub in subChaptersToken)
            {
                JToken subChapter = ResolveRef(sub["Chapter"], idMap);
                if (subChapter != null)
                    CollectSubChapterRecursive(subChapter, chapters, steps, idMap, guidToName, indentLevel + 1);
            }
        }
    }
}




    private static Dictionary<string, string> BuildGuidToNameMap(JToken root)
    {
        Dictionary<string, string> map = new();
        foreach (var stepToken in root.SelectTokens("$..[?(@.$type && @.$type =~ /VRBuilder\\.Core\\.Step/)]"))
        {
            var guid = stepToken.SelectToken("StepMetadata.Guid")?.ToString();
            var name = stepToken.SelectToken("Data.Name")?.ToString();
            if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(name))
                map[guid] = name;
        }
        return map;
    }

    private static void BuildIdMap(JToken token, Dictionary<string, JToken> map)
    {
        if (token is JObject obj)
        {
            if (obj.TryGetValue("$id", out JToken idToken))
            {
                string id = idToken.ToString();
                if (!map.ContainsKey(id))
                    map[id] = obj;
            }
            foreach (var prop in obj.Properties())
                BuildIdMap(prop.Value, map);
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
                BuildIdMap(item, map);
        }
    }

    private static JToken ResolveRef(JToken token, Dictionary<string, JToken> idMap)
    {
        if (token == null)
            return null;

        if (token is JObject obj && obj.TryGetValue("$ref", out JToken refId))
        {
            idMap.TryGetValue(refId.ToString(), out JToken resolved);
            return resolved;
        }

        return token;
    }
}
