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

    [Header("Emergency Reset")]
    [SerializeField] private KeyCode emergencyResetKey = KeyCode.F12;

    [Header("Controllo globale")]
    [Tooltip("Se disattivato, i Learning Style Features vengono ignorati.")]
    public bool enableLearningFeatures = true;

    private void Awake()
    {
        if (SessionManager.Instance != null)
        {
            var sel = SessionManager.Instance.SelectedLearningProfile;
            if (sel != null)
            {
                attivoRiflessivo       = sel.attivoRiflessivo;
                sensitivoIntuitivo     = sel.sensitivoIntuitivo;
                visivoVerbale          = sel.visivoVerbale;
                sequenzialeGlobale     = sel.sequenzialeGlobale;
                enableLearningFeatures = sel.enableLearningFeatures; // ← NUOVO
    
                Debug.Log($"[LearningProfile] Profilo applicato dal menu: " +
                        $"{attivoRiflessivo} | {sensitivoIntuitivo} | " +
                        $"{visivoVerbale} | {sequenzialeGlobale} | " +
                        $"enableLearningFeatures={enableLearningFeatures}");
            }
        }
    }

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
            Debug.Log("[LearningProfile] Learning features disattivate globalmente.");
            return null;
        }

        return attivoRiflessivo == LearningEnums.AttivoRiflessivo.Riflessivo
            ? riflessivoFeatures
            : attivoFeatures;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(emergencyResetKey)) return;

        var behaviour = GetCurrentBehaviour();
        if (behaviour is RiflessivoFeatures rf)
        {
            Debug.LogWarning("[LearningProfile] Emergency reset attivato da tastiera.");
            rf.EmergencyReset();
        }
    }
}