using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SessionPersistence
{
    private const string KEY = "session_id";

    public static void Save(string sessionId)
    {
        Debug.Log("Sessione saved");
        PlayerPrefs.SetString(KEY, sessionId);
        PlayerPrefs.Save();
    }

    public static string Load()
        => PlayerPrefs.GetString(KEY, null);

    public static void Clear()
    {
        Debug.Log("Sessione cleaned");
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }

    public static bool HasSavedSession()
        => PlayerPrefs.HasKey(KEY);
}