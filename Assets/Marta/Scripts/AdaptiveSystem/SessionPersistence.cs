using UnityEngine;

public static class SessionPersistence
{
    private const string KEY = "session_id";
    private const string KEY_RESET = "reset_all";
    private const string KEY_USER_PREFIX = "user_prefix"; // ← NUOVO

    // ===== SESSION ID =====
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

    // ===== USER PREFIX =====
    public static void SaveUserPrefix(string prefix)
    {
        PlayerPrefs.SetString(KEY_USER_PREFIX, prefix.Trim());
        PlayerPrefs.Save();
    }

    public static string LoadUserPrefix()
        => PlayerPrefs.GetString(KEY_USER_PREFIX, "");

    public static void ClearUserPrefix()
    {
        PlayerPrefs.DeleteKey(KEY_USER_PREFIX);
        PlayerPrefs.Save();
    }

    // ===== RESET FLAG =====
    public static void SetResetAll(bool value)
    {
        PlayerPrefs.SetInt(KEY_RESET, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool GetResetAll()
        => PlayerPrefs.GetInt(KEY_RESET, 1) == 1;
}