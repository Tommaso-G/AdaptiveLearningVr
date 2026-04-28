using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackIconController : MonoBehaviour
{
    [Header("Visivo")]
    public Sprite visivoSprite;

    [Header("Verbale")]
    [TextArea]
    public string verbaleText;

    private Transform feedbackRoot;
    private Transform visivoObj;
    private Transform verbaleObj;

    void Awake()
    {
        Debug.Log($"[FeedbackIconController] Init su {gameObject.name}");

        feedbackRoot = FindChildWithTag(transform, "IconFeedback");

        if (feedbackRoot == null)
        {
            Debug.LogError("❌ FeedbackIcons NON trovato!");
            return;
        }

        visivoObj = FindChildByName(feedbackRoot, "Visivo");
        verbaleObj = FindChildByName(feedbackRoot, "Verbale");

        Debug.Log($"Visivo: {(visivoObj ? "OK" : "MISSING")}");
        Debug.Log($"Verbale: {(verbaleObj ? "OK" : "MISSING")}");
    }

    public void SetMode(bool isVisivo)
    {
        Debug.Log($"[SetMode] {(isVisivo ? "Visivo" : "Verbale")}");

        if (visivoObj == null || verbaleObj == null)
            return;

        visivoObj.gameObject.SetActive(isVisivo);
        verbaleObj.gameObject.SetActive(!isVisivo);

        if (isVisivo)
        {
            Image img = visivoObj.GetComponentInChildren<Image>(true);

            if (img == null)
            {
                Debug.LogError("❌ Image NON trovata in Visivo (anche nei figli)");
                return;
            }

            Debug.Log($"✅ Image trovata su: {GetFullPath(img.transform)}");
            img.sprite = visivoSprite;
        }
        else
        {
            // 🔥 QUI la parte importante
            TMP_Text tmp = verbaleObj.GetComponentInChildren<TMP_Text>(true);

            if (tmp == null)
            {
                Debug.LogError("❌ TMP_Text NON trovato in Verbale (neanche nei figli!)");
                
                // DEBUG EXTRA: lista componenti
                DebugComponents(verbaleObj);
                return;
            }

            Debug.Log($"✅ TMP_Text trovato su: {GetFullPath(tmp.transform)}");
            tmp.text = verbaleText;
        }
    }

    // 🔍 Ricorsiva TAG
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
                return child;
        }
        return null;
    }

    // 🔍 Ricorsiva NOME
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    // 🧭 Path completo
    private string GetFullPath(Transform obj)
    {
        string path = obj.name;
        while (obj.parent != null)
        {
            obj = obj.parent;
            path = obj.name + "/" + path;
        }
        return path;
    }

    // 🧪 Debug componenti presenti
    private void DebugComponents(Transform obj)
    {
        Debug.Log("🔎 Componenti su Verbale:");
        foreach (var comp in obj.GetComponentsInChildren<Component>(true))
        {
            Debug.Log($"- {comp.GetType()} su {GetFullPath(comp.transform)}");
        }
    }
}