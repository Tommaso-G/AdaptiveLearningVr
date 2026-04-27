using System;
using System.Collections.Generic;
using UnityEngine;
using static FeedbackRepository;

/// <summary>
/// Gestore dei feedback eccezionali.
/// Mettilo sullo stesso GameObject di LearningProfile.
/// Configura la lista exceptionSlots nell'Inspector, poi chiama
/// SetActive("FeedbackName") per abilitare uno slot e
/// ShowExceptionFeedback("FeedbackName") per mostrarlo.
/// </summary>
public class ExceptionFeedbackHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // CLASSE SLOT
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public class ExceptionFeedbackSlot
    {
        [Tooltip("FeedbackName del feedback da mostrare (deve coincidere con FeedbackData.FeedbackName nel repository).")]
        public string feedbackName = "";

        [Tooltip("GameObject della scena su cui verrà cercato il 'feedbackPosition' per istanziare il feedback.")]
        public GameObject targetObject;

        [Tooltip("Se false, ShowExceptionFeedback ignorerà questo slot anche se chiamato.")]
        public bool isActive = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // CAMPI INSPECTOR
    // ─────────────────────────────────────────────────────────────────

    [Header("Riferimenti")]
    public FeedbackRepository repository;
    public FeedbackSetHolder feedbackHolder;
    public FeedbackDisplayer feedbackDisplayer;

    [Tooltip("Filtro per capitolo. Opzionale: se assegnato, controlla il livello di feedback del capitolo che contiene il feedback richiesto.")]
    public FeedbackChapterFilter chapterFilter;

    [Header("Feedback Eccezionali")]
    public List<ExceptionFeedbackSlot> exceptionSlots = new List<ExceptionFeedbackSlot>();

    // ─────────────────────────────────────────────────────────────────
    // STATO INTERNO
    // ─────────────────────────────────────────────────────────────────

    private readonly Dictionary<string, List<GameObject>> _activeFeedbacks
        = new Dictionary<string, List<GameObject>>();


    // ─────────────────────────────────────────────────────────────────
    // API PUBBLICA
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abilita lo slot con il feedbackName specificato, permettendo a
    /// ShowExceptionFeedback di mostrarlo.
    /// </summary>
    public void SetActive(string feedbackName)
    {
        ExceptionFeedbackSlot slot = exceptionSlots.Find(s =>
            string.Equals(s.feedbackName, feedbackName, StringComparison.OrdinalIgnoreCase));

        if (slot == null)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] SetActive: nessuno slot trovato per '{feedbackName}'.");
            return;
        }

        slot.isActive = true;
        Debug.Log($"[ExceptionFeedbackHandler] Slot '{feedbackName}' attivato.");
    }

    /// <summary>
    /// Disabilita lo slot con il feedbackName specificato.
    /// </summary>
    public void SetInactive(string feedbackName)
    {
        ExceptionFeedbackSlot slot = exceptionSlots.Find(s =>
            string.Equals(s.feedbackName, feedbackName, StringComparison.OrdinalIgnoreCase));

        if (slot == null)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] SetInactive: nessuno slot trovato per '{feedbackName}'.");
            return;
        }

        slot.isActive = false;
        Debug.Log($"[ExceptionFeedbackHandler] Slot '{feedbackName}' disattivato.");
    }

    /// <summary>
    /// Mostra il feedback eccezionale con il nome specificato, solo se lo slot è attivo.
    /// </summary>
    public void ShowExceptionFeedback(string feedbackName)
    {
        if (!ValidateReferences()) return;

        // 1️⃣ Trova lo slot configurato per questo feedbackName
        ExceptionFeedbackSlot slot = exceptionSlots.Find(s =>
            string.Equals(s.feedbackName, feedbackName, StringComparison.OrdinalIgnoreCase));

        if (slot == null)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] Nessuno slot configurato per '{feedbackName}'.");
            return;
        }

        // 2️⃣ Controlla isActive
        if (!slot.isActive)
        {
            Debug.Log($"[ExceptionFeedbackHandler] Slot '{feedbackName}' non attivo, feedback ignorato.");
            return;
        }

        if (slot.targetObject == null)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] Lo slot '{feedbackName}' non ha un targetObject assegnato.");
            return;
        }

        // 3️⃣ Ricava il profilo
        LearningProfile learningProfile = GetComponent<LearningProfile>();
        if (learningProfile == null)
        {
            Debug.LogError("[ExceptionFeedbackHandler] LearningProfile non trovato sul GameObject.");
            return;
        }
        var profile = learningProfile.GetProfileTuple();

        // 4️⃣ Ricava il chapterName (serve per il filtro e per decidere se il waypoint è opzionale)
        string chapterName = repository.GetExceptionChapterName(feedbackName, profile);

        // 5️⃣ Controlla il FeedbackChapterFilter usando il capitolo che contiene il feedback
        if (chapterFilter != null)
        {
            if (!string.IsNullOrEmpty(chapterName) && !chapterFilter.IsFeedbackAllowed(chapterName))
            {
                Debug.Log($"[ExceptionFeedbackHandler] Feedback '{feedbackName}' bloccato (capitolo '{chapterName}').");
                return;
            }
        }

        // 5️⃣ Recupera il FeedbackData dal repository
        FeedbackData data = repository.GetExceptionFeedback(feedbackName, profile);

        if (data == null)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] Nessun FeedbackData trovato per '{feedbackName}' con il profilo corrente.");
            return;
        }

        // 6️⃣ Ricava le posizioni di spawn dal targetObject
        List<Transform> positions = feedbackDisplayer.FindFeedbackPositionChild(slot.targetObject);

        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] Nessuna feedbackPosition trovata su '{slot.targetObject.name}'.");
            return;
        }

        // 7️⃣ Istanzia e popola il prefab
        feedbackDisplayer.ChooseFeedback(data, positions, feedbackHolder);

        // 8️⃣ Segna il feedback come opzionale se il capitolo è opzionale,
        //    prima che Start() venga eseguito così FeedbackPrefabController
        //    istanzierà OptionalWayPoint invece di waypointPrefab
        GameObject instance = feedbackHolder.activeFeedbackInstance;
        if (instance != null)
        {
            FeedbackPrefabController controller = instance.GetComponent<FeedbackPrefabController>();
            if (controller != null)
                controller.isOptionalFeedback = chapterName != null && chapterName.Contains("Optional");
        }

        // 9️⃣ Registra l'istanza attiva
        instance = feedbackHolder.activeFeedbackInstance;

        if (instance != null)
        {
            if (!_activeFeedbacks.ContainsKey(feedbackName))
                _activeFeedbacks[feedbackName] = new List<GameObject>();

            _activeFeedbacks[feedbackName].Add(instance);

            Debug.Log($"[ExceptionFeedbackHandler] Feedback '{feedbackName}' mostrato.");
        }
        else
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] FeedbackDisplayer non ha prodotto un'istanza per '{feedbackName}'.");
        }
    }

    /// <summary>
    /// Chiude tutti i feedback attivi con il nome specificato,
    /// usando FeedbackPrefabController.CloseFeedback() come fa FeedbackAutoManager.
    /// </summary>
    public void HideExceptionFeedback(string feedbackName)
    {
        if (!_activeFeedbacks.TryGetValue(feedbackName, out var instances))
        {
            Debug.LogWarning($"[ExceptionFeedbackHandler] Nessun feedback attivo per '{feedbackName}'.");
            return;
        }

        // Usa CloseFeedback() per chiudere correttamente (gestisce waypoint e animazioni)
        FeedbackPrefabController[] allControllers = FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);
        foreach (var controller in allControllers)
        {
            if (controller.name.Contains(feedbackName))
                controller.CloseFeedback();
        }

        _activeFeedbacks.Remove(feedbackName);
        Debug.Log($"[ExceptionFeedbackHandler] Feedback '{feedbackName}' chiusi.");
    }

    /// <summary>
    /// Chiude tutti i feedback eccezionali attualmente visibili.
    /// </summary>
    public void HideAllExceptionFeedbacks()
    {
        foreach (var pair in _activeFeedbacks)
        {
            FeedbackPrefabController[] allControllers = FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);
            foreach (var controller in allControllers)
            {
                if (controller.name.Contains(pair.Key))
                    controller.CloseFeedback();
            }
        }

        _activeFeedbacks.Clear();
        Debug.Log("[ExceptionFeedbackHandler] Tutti i feedback eccezionali chiusi.");
    }

    /// <summary>
    /// Restituisce true se esiste almeno un'istanza attiva per il feedbackName specificato.
    /// </summary>
    public bool IsActive(string feedbackName)
    {
        return _activeFeedbacks.TryGetValue(feedbackName, out var list) && list.Count > 0;
    }


    // ─────────────────────────────────────────────────────────────────
    // METODI PRIVATI
    // ─────────────────────────────────────────────────────────────────

    private bool ValidateReferences()
    {
        if (repository == null)
        {
            Debug.LogError("[ExceptionFeedbackHandler] FeedbackRepository non assegnato.");
            return false;
        }
        if (feedbackHolder == null)
        {
            Debug.LogError("[ExceptionFeedbackHandler] FeedbackSetHolder non assegnato.");
            return false;
        }
        if (feedbackDisplayer == null)
        {
            Debug.LogError("[ExceptionFeedbackHandler] FeedbackDisplayer non assegnato.");
            return false;
        }
        return true;
    }
}