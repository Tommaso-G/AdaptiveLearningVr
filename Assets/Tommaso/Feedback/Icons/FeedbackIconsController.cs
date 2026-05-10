using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackIconController : MonoBehaviour
{
    [Header("Visivo")]
    public Sprite visivoSprite;

    [Header("Verbale")]
    public string verbaleText;

    private Transform feedbackRoot;
    private Transform visivoObj;
    private Transform verbaleObj;

    private bool isInitialized = false;

    // Salva l'ultimo modo richiesto
    private bool pendingMode;
    private bool hasPendingMode = false;

    // Modalità corrente
    private bool currentMode = true;

    void OnEnable()
    {
        TryInit();

        if (hasPendingMode)
        {
            ApplyMode(pendingMode);
            hasPendingMode = false;
        }
    }

    private bool TryInit()
    {
        if (isInitialized)
            return true;

        feedbackRoot = FindChildWithTag(transform, "IconFeedback");

        if (feedbackRoot == null)
        {
            //Debug.LogError($"❌ IconFeedback non trovato su {gameObject.name}");
            return false;
        }

        visivoObj = FindChildByName(feedbackRoot, "Visivo");
        verbaleObj = FindChildByName(feedbackRoot, "Verbale");

        isInitialized = (visivoObj != null && verbaleObj != null);

        return isInitialized;
    }

    public void SetMode(bool isVisivo)
    {
        if (TryInit())
        {
            ApplyMode(isVisivo);
        }
        else
        {
            // Oggetto disattivato → salva per dopo
            pendingMode = isVisivo;
            hasPendingMode = true;

            //Debug.Log($"[FeedbackIconController] {gameObject.name} disattivato, modo '{isVisivo}' salvato.");
        }
    }

    /// <summary>
    /// Rilegge visivoSprite e verbaleText
    /// e aggiorna la UI corrente.
    /// </summary>
    public void Refresh()
    {
        if (TryInit())
        {
            ApplyMode(currentMode);
        }
    }

    private void ApplyMode(bool isVisivo)
    {
        currentMode = isVisivo;

        visivoObj.gameObject.SetActive(isVisivo);
        verbaleObj.gameObject.SetActive(!isVisivo);

        if (isVisivo)
        {
            var img = visivoObj.GetComponentInChildren<Image>(true);

            if (img == null)
            {
                //Debug.LogError("❌ Image non trovata in Visivo");
                return;
            }

            img.sprite = visivoSprite;
        }
        else
        {
            var tmp = verbaleObj.GetComponentInChildren<TMP_Text>(true);

            if (tmp == null)
            {
                //Debug.LogError("❌ TMP_Text non trovato in Verbale");
                //DebugComponents(verbaleObj);
                return;
            }

            tmp.text = verbaleText;
        }
    }

    // ─────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────

    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
                return child;
        }

        return null;
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

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

    private void DebugComponents(Transform obj)
    {
        Debug.Log("🔎 Componenti su Verbale:");

        foreach (var comp in obj.GetComponentsInChildren<Component>(true))
        {
            Debug.Log($"  - {comp.GetType()} su {GetFullPath(comp.transform)}");
        }
    }
}