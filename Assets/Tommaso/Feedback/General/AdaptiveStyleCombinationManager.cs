using UnityEngine;

public class AdaptiveStyleCombinationManager : MonoBehaviour
{
    [Header("Riferimento al profilo")]
    public LearningProfile learningProfile;

    [Header("Combinazioni stili (8)")]

    [Tooltip("Visivo - Attivo - Globale")]
    public GameObject visivoAttivoGlobale;

    [Tooltip("Visivo - Attivo - Sequenziale")]
    public GameObject visivoAttivoSequenziale;

    [Tooltip("Visivo - Riflessivo - Globale")]
    public GameObject visivoRiflessivoGlobale;

    [Tooltip("Visivo - Riflessivo - Sequenziale")]
    public GameObject visivoRiflessivoSequenziale;

    [Tooltip("Verbale - Attivo - Globale")]
    public GameObject verbaleAttivoGlobale;

    [Tooltip("Verbale - Attivo - Sequenziale")]
    public GameObject verbaleAttivoSequenziale;

    [Tooltip("Verbale - Riflessivo - Globale")]
    public GameObject verbaleRiflessivoGlobale;

    [Tooltip("Verbale - Riflessivo - Sequenziale")]
    public GameObject verbaleRiflessivoSequenziale;

    private GameObject[] allObjects;

    void Start()
    {
        if (learningProfile == null)
        {
            Debug.LogError("LearningProfile non assegnato!");
            return;
        }

        // Salva tutti gli oggetti in un array
        allObjects = new GameObject[]
        {
            visivoAttivoGlobale,
            visivoAttivoSequenziale,
            visivoRiflessivoGlobale,
            visivoRiflessivoSequenziale,
            verbaleAttivoGlobale,
            verbaleAttivoSequenziale,
            verbaleRiflessivoGlobale,
            verbaleRiflessivoSequenziale
        };

        // Disattiva tutto
        SetAllActive(false);

        // Recupera il profilo
        var profile = learningProfile.GetProfileTuple();

        GameObject targetObject = GetMatchingObject(profile);

        // Attiva solo l'oggetto corretto
        if (targetObject != null)
        {
            targetObject.SetActive(true);
            Debug.Log($"[AdaptiveStyleCombinationManager] Attivato: {targetObject.name}");
        }
        else
        {
            Debug.LogWarning("[AdaptiveStyleCombinationManager] Nessuna combinazione trovata.");
        }
    }

    GameObject GetMatchingObject(
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
         LearningEnums.SensitivoIntuitivo sensitivoIntuitivo,
         LearningEnums.VisivoVerbale visivoVerbale,
         LearningEnums.SequenzialeGlobale sequenzialeGlobale) profile)
    {
        bool isVisivo =
            profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;

        bool isAttivo =
            profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;

        bool isGlobale =
            profile.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale;

        // VISIVO
        if (isVisivo)
        {
            if (isAttivo)
            {
                return isGlobale
                    ? visivoAttivoGlobale
                    : visivoAttivoSequenziale;
            }
            else
            {
                return isGlobale
                    ? visivoRiflessivoGlobale
                    : visivoRiflessivoSequenziale;
            }
        }

        // VERBALE
        else
        {
            if (isAttivo)
            {
                return isGlobale
                    ? verbaleAttivoGlobale
                    : verbaleAttivoSequenziale;
            }
            else
            {
                return isGlobale
                    ? verbaleRiflessivoGlobale
                    : verbaleRiflessivoSequenziale;
            }
        }
    }

    void SetAllActive(bool state)
    {
        foreach (var obj in allObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}