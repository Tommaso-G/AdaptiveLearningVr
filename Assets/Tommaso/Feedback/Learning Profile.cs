using UnityEngine;

public class LearningProfile : MonoBehaviour
{
    [Header("Stile di Apprendimento (FSLSM)")]
    public LearningEnums.AttivoRiflessivo attivoRiflessivo = LearningEnums.AttivoRiflessivo.Attivo;
    public LearningEnums.SensitivoIntuitivo sensitivoIntuitivo = LearningEnums.SensitivoIntuitivo.Sensitivo;
    public LearningEnums.VisivoVerbale visivoVerbale = LearningEnums.VisivoVerbale.Visivo;
    public LearningEnums.SequenzialeGlobale sequenzialeGlobale = LearningEnums.SequenzialeGlobale.Sequenziale;

    [Header("Comportamenti associati")]
    public LearningStyleFeatures riflessivoFeatures;
    public LearningStyleFeatures attivoFeatures;

    [Header("Controllo globale")]
    [Tooltip("Se disattivato, i Learning Style Features vengono ignorati.")]
    public bool enableLearningFeatures = true;

    public (LearningEnums.AttivoRiflessivo attivoRiflessivo,
            LearningEnums.SensitivoIntuitivo sensitivoIntuitivo,
            LearningEnums.VisivoVerbale visivoVerbale,
            LearningEnums.SequenzialeGlobale sequenzialeGlobale)
        GetProfileTuple()
    {
        return (attivoRiflessivo, sensitivoIntuitivo, visivoVerbale, sequenzialeGlobale);
    }

    public LearningStyleFeatures GetCurrentBehaviour()
    {
        if (!enableLearningFeatures)
        {
            Debug.Log("[LearningProfile] Learning features disattivate globalmente — nessun comportamento applicato.");
            return null;
        }

        switch (attivoRiflessivo)
        {
            case LearningEnums.AttivoRiflessivo.Riflessivo:
                return riflessivoFeatures;
            case LearningEnums.AttivoRiflessivo.Attivo:
            default:
                return attivoFeatures;
        }
    }
}
