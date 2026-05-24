using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private TMP_InputField NameInputField;
    [SerializeField] private TMP_InputField PathInputField;

    public void ApriScena(string nomeScena)
    {
        PlayerPrefs.SetString("ProfilingSessionName", NameInputField.text);
        PlayerPrefs.SetString("ProfilingSessionPath", PathInputField.text);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nomeScena);
    }
}