using System.Collections.Generic;
using UnityEngine;

// ── Tipi top-level (necessari perché CustomPropertyDrawer funzioni) ────────

[System.Serializable]
public class OptionalChapterEntry
{
    public string chapterName;
}

[System.Serializable]
public class ChapterFeedbackOverride
{
    public string chapterName;

    [Range(0, 2)]
    [Tooltip("0 = feedback completo, 1 = solo outline, 2 = nessun feedback")]
    public int feedbackLevel = 0;
}

[System.Serializable]
public class ChapterDifficultyOverride
{
    public string chapterName;

    [Range(0, 1)]
    [Tooltip("0 = base, 1 = avanzato")]
    public int difficultyLevel = 0;
}

// ── ScriptableObject ──────────────────────────────────────────────────────

/// <summary>
/// ScriptableObject che descrive un livello della modalità offline.
/// Crea asset via: tasto destro → Create → OfflineMode → Level Config
/// </summary>
[CreateAssetMenu(menuName = "OfflineMode/Level Config", fileName = "OfflineLevel")]
public class OfflineLevelConfig : ScriptableObject
{
    [Tooltip("Nome del livello mostrato nel menu (es. 'Livello 1 - Principiante')")]
    public string levelName = "Livello";

    [Tooltip("Descrizione opzionale mostrata nel menu")]
    [TextArea(1, 3)]
    public string description = "";

    [Tooltip("Se true, il pannello progressi mostra il messaggio iniziale invece degli errori precedenti. "
             + "Attivare solo per il primo livello del registry.")]
    public bool isFirstLevel = false;

    [Tooltip("Capitoli opzionali da aggiungere in questo livello.")]
    public List<OptionalChapterEntry> optionalChaptersToAdd = new List<OptionalChapterEntry>();

    [Tooltip("Impostazioni di feedback per ogni capitolo.")]
    public List<ChapterFeedbackOverride> feedbackOverrides = new List<ChapterFeedbackOverride>();

    [Tooltip("Impostazioni di difficoltà per ogni capitolo.")]
    public List<ChapterDifficultyOverride> difficultyOverrides = new List<ChapterDifficultyOverride>();
}