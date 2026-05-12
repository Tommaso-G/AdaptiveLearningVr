using System.Collections.Generic;

[System.Serializable]
public class SessionJson
{
    public string session_id;
    public int iteration;
    public List<ChapterJson> chapters = new List<ChapterJson>();
}

[System.Serializable]
public class ChapterJson
{
    public string chapter_id;
    public string chapter_name;
    public float time_seconds;
    public List<StepError> errors = new List<StepError>();
}