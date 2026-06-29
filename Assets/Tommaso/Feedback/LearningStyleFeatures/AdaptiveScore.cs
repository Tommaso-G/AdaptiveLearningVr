using UnityEngine;

/// <summary>
/// Mantiene e aggiorna uno score continuo [0,1] che rappresenta
/// la confidenza che il comportamento dell'utente sia "Riflessivo".
///
///   0.0 – 0.2  →  Fortemente Attivo
///   0.2 – 0.4  →  Tendenzialmente Attivo
///   0.4 – 0.6  →  Misto
///   0.6 – 0.8  →  Tendenzialmente Riflessivo
///   0.8 – 1.0  →  Fortemente Riflessivo
///
/// Persiste tra i livelli offline tramite SessionManager.
/// </summary>
[System.Serializable]
public class AdaptiveScore
{
    // ── Score ──────────────────────────────────────────────────────────────
    [Range(0f, 1f)]
    [Tooltip("Score corrente [0=Attivo, 1=Riflessivo]. Modificabile da Inspector per test.")]
    public float score = 0.5f;

    // ── Delta per segnale ──────────────────────────────────────────────────
    [Header("Delta per segnale")]
    [Tooltip("Quanto aumenta lo score per ogni feedback ad alto engagement.")]
    public float deltaHighFeedback = 0.1f;

    [Tooltip("Quanto diminuisce lo score per ogni feedback a basso engagement.")]
    public float deltaLowFeedback = 0.12f;

    [Tooltip("Quanto aumenta lo score per ogni capitolo lento.")]
    public float deltaSlowChapter = 0.15f;

    // ── Soglie di switch ───────────────────────────────────────────────────
    [Header("Soglie di switch")]
    [Tooltip("Score minimo per switchare ad Attivo (se si era Riflessivo).")]
    [Range(0f, 1f)]
    public float attivoThreshold = 0.35f;

    [Tooltip("Score minimo per switchare a Riflessivo (se si era Attivo).")]
    [Range(0f, 1f)]
    public float riflessivoThreshold = 0.65f;

    // ── API ────────────────────────────────────────────────────────────────

    public void ApplyHighFeedback()  => Apply(+deltaHighFeedback, "feedback alto engagement");
    public void ApplyLowFeedback()   => Apply(-deltaLowFeedback,  "feedback basso engagement");

    /// <summary>
    /// Applica un delta continuo basato su quanto tempo ha impiegato il capitolo
    /// rispetto al midEventTime del ChapterTimer.
    ///
    /// slowRatio = time / midEventTime:
    ///   < 0.50  → veloce     → delta negativo (score scende)
    ///   0.50    → normale    → delta neutro (0)
    ///   0.50–0.75 → normale  → interpolazione lineare 0 → 0
    ///   0.75    → lento      → delta positivo piccolo
    ///   0.75–1.0  → lento    → interpolazione lineare verso +deltaSlowChapter
    ///   >= 1.0  → lentissimo → delta massimo positivo (+deltaSlowChapter), scalato oltre 1
    ///
    /// Zone:
    ///   [0,    0.50] → mappa a [-deltaSlowChapter, 0]
    ///   [0.50, 0.75] → delta = 0  (zona neutra)
    ///   [0.75, 1.0 ] → mappa a [0, +deltaSlowChapter]
    ///   [1.0,  2.0 ] → mappa a [+deltaSlowChapter, +deltaSlowChapter*2], clampato al massimo
    /// </summary>
    public void ApplySlowChapterContinuous(float slowRatio)
    {
        float delta;

        if (slowRatio < 0.50f)
        {
            // Veloce: da 0 a 0.50 → da -deltaSlowChapter a 0
            float t = slowRatio / 0.50f;           // [0,1]
            delta = Mathf.Lerp(-deltaSlowChapter, 0f, t);
        }
        else if (slowRatio < 0.75f)
        {
            // Normale: zona neutra
            delta = 0f;
        }
        else if (slowRatio < 1.0f)
        {
            // Lento: da 0.75 a 1.0 → da 0 a +deltaSlowChapter
            float t = (slowRatio - 0.75f) / 0.25f; // [0,1]
            delta = Mathf.Lerp(0f, deltaSlowChapter, t);
        }
        else
        {
            // Lentissimo: oltre 1.0, scala ulteriormente fino a 2x il delta, clampato
            float t = Mathf.Clamp01(slowRatio - 1.0f); // [0,1] per ratio in [1,2]
            delta = Mathf.Lerp(deltaSlowChapter, deltaSlowChapter * 2f, t);
        }

        Apply(delta, $"capitolo continuo (ratio={slowRatio:F2}, delta={delta:+0.000;-0.000})");
    }

    private void Apply(float delta, string reason)
    {
        float before = score;
        score = Mathf.Clamp01(score + delta);
        Debug.Log($"[AdaptiveScore] {reason}: {before:F2} → {score:F2} (delta={delta:+0.00;-0.00})  {Interpret()}");
    }

    /// <summary>Restituisce true se lo score ha superato la soglia Riflessivo.</summary>
    public bool ShouldSwitchToRiflessivo() => score >= riflessivoThreshold;

    /// <summary>Restituisce true se lo score è sceso sotto la soglia Attivo.</summary>
    public bool ShouldSwitchToAttivo() => score <= attivoThreshold;

    public string Interpret()
    {
        if (score < 0.2f) return "[Fortemente Attivo]";
        if (score < 0.4f) return "[Tendenzialmente Attivo]";
        if (score < 0.6f) return "[Misto]";
        if (score < 0.8f) return "[Tendenzialmente Riflessivo]";
        return "[Fortemente Riflessivo]";
    }
}