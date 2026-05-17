using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void ApriScena(string nomeScena)
    {
        PlayerPrefs.SetString("ProfilingSessionName", inputField.text);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nomeScena);
    }
}