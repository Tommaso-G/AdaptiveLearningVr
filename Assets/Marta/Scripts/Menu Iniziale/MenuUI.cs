// MenuUI.cs (nella scena MenuScene)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button newSessionButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI sessionInfoText;
    [SerializeField] private TextMeshProUGUI instructionsText;

    private void Start()
    {
        Outline[] outlines = FindObjectsByType<Outline>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var outline in outlines)
        {
            outline.enabled = false;
            Debug.Log($"[MenuUI] Outline disabilitato su {outline.gameObject.name}");
        }


        newSessionButton.onClick.AddListener(OnNewSessionClicked);
        continueButton.onClick.AddListener(OnContinueClicked);

        // Disabilita continua se non c'è una sessione salvata
        bool hasSavedSession = SessionPersistence.HasSavedSession();
        continueButton.interactable = hasSavedSession;

        if (hasSavedSession)
        {
            string sessionId = SessionPersistence.Load();
            sessionInfoText.text = $"Sessione salvata: {sessionId}";
        }
        else
        {
            sessionInfoText.text = "Nessuna sessione salvata";
        }

        instructionsText.text = "Scegli un'opzione per iniziare:";
    }

    private void OnNewSessionClicked()
    {
        Debug.Log("[MenuUI] Nuova sessione avviata");
        SessionManager.Instance.StartNewSession();
    }

    private void OnContinueClicked()
    {
        if (SessionPersistence.HasSavedSession())
        {
            Debug.Log("[MenuUI] Continuazione sessione");
            SessionManager.Instance.ContinueSession();
        }
        else
        {
            Debug.LogWarning("[MenuUI] Nessuna sessione da continuare!");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            OnNewSessionClicked();
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            OnContinueClicked();
        }
    }
}