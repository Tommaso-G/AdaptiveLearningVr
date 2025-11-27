using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeedbackSet", menuName = "VR Feedback/Feedback Set")]
public class FeedbackSet : ScriptableObject
{
    [Header("Lista di feedback disponibili")]
    public List<FeedbackData> feedbackDataList = new List<FeedbackData>();

    /// <summary>
    /// Trova e restituisce il feedback giusto in base al profilo utente.
    /// </summary>
    public FeedbackData GetMatchingFeedback(LearningProfile profile, int Step)
    {
        foreach (var feedback in feedbackDataList)
        {
            if (feedback.attivoRiflessivo.ToString() == profile.attivoRiflessivo.ToString() &&
                feedback.sensitivoIntuitivo.ToString() == profile.sensitivoIntuitivo.ToString() &&
                feedback.visivoVerbale.ToString() == profile.visivoVerbale.ToString() &&
                feedback.sequenzialeGlobale.ToString() == profile.sequenzialeGlobale.ToString() && 
                feedback.Step == Step) 
            {
                return feedback;
            }
        }

        Debug.Log($"[FeedbackSet] Nessun feedback trovato per il profilo utente nel set: o non ci deve essere o hai sbagliato tu qualcosa {name}.");
        return null;
    }
}
