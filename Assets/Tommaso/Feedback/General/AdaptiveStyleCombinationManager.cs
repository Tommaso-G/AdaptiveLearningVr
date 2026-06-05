using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class StyleCombination
{
    [Header("Contenuto")]
    public GameObject content;

    [Header("Nav Panel")]
    public GameObject navPanel;
}

public class AdaptiveStyleCombinationManager : MonoBehaviour
{
    [Header("Riferimento al profilo")]
    public LearningProfile learningProfile;

    [Header("Combinazioni stili (8)")]
    public StyleCombination visivoAttivoGlobale;
    public StyleCombination visivoAttivoSequenziale;
    public StyleCombination visivoRiflessivoGlobale;
    public StyleCombination visivoRiflessivoSequenziale;

    public StyleCombination verbaleAttivoGlobale;
    public StyleCombination verbaleAttivoSequenziale;
    public StyleCombination verbaleRiflessivoGlobale;
    public StyleCombination verbaleRiflessivoSequenziale;

    [Header("Scrollable")]
    public ScrollRect scrollRect;

    private StyleCombination[] allCombinations;

    void Start()
    {
        if (learningProfile == null)
        {
            Debug.LogError("LearningProfile non assegnato!");
            return;
        }

        allCombinations = new StyleCombination[]
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

        SetAllActive(false);

        var profile = learningProfile.GetProfileTuple();
        StyleCombination target = GetMatchingCombination(profile);

        if (target != null)
        {
            if (target.content != null)
            {
                target.content.SetActive(true);

                if (scrollRect != null)
                {
                    RectTransform contentRect = target.content.GetComponent<RectTransform>();
                    if (contentRect != null)
                    {
                       scrollRect.content = contentRect;
                    }
                }
            }

            if (target.navPanel != null)
            {
                target.navPanel.SetActive(true);
                SetupToggleListeners(target.navPanel);
            }

            Debug.Log($"[AdaptiveStyleCombinationManager] Attivata combinazione: {target.content?.name}");
        }
    }

    StyleCombination GetMatchingCombination(
        (LearningEnums.AttivoRiflessivo attivoRiflessivo,
         LearningEnums.SensitivoIntuitivo sensitivoIntuitivo,
         LearningEnums.VisivoVerbale visivoVerbale,
         LearningEnums.SequenzialeGlobale sequenzialeGlobale) profile)
    {
        bool isVisivo = profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;
        bool isAttivo = profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;
        bool isGlobale = profile.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale;

        if (isVisivo)
        {
            if (isAttivo)
                return isGlobale ? visivoAttivoGlobale : visivoAttivoSequenziale;
            else
                return isGlobale ? visivoRiflessivoGlobale : visivoRiflessivoSequenziale;
        }
        else
        {
            if (isAttivo)
                return isGlobale ? verbaleAttivoGlobale : verbaleAttivoSequenziale;
            else
                return isGlobale ? verbaleRiflessivoGlobale : verbaleRiflessivoSequenziale;
        }
    }

    private void SetupToggleListeners(GameObject navPanel)
    {
        Toggle[] toggles = navPanel.GetComponentsInChildren<Toggle>(true);

        foreach (Toggle toggle in toggles)
        {
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    StartCoroutine(RefreshScrollRect());
                }
            });
        }
    }

    private IEnumerator RefreshScrollRect()
    {
        if (scrollRect == null)
            yield break;

        scrollRect.enabled = false;

        yield return null; // aspetta un frame

        scrollRect.enabled = true;

        Debug.Log("Chiamato refresh scrioll bar");
    }

    void SetAllActive(bool state)
    {
        if (allCombinations == null) return;

        foreach (var combo in allCombinations)
        {
            if (combo == null) continue;
            if (combo.content != null) combo.content.SetActive(state);
            if (combo.navPanel != null) combo.navPanel.SetActive(state);
        }
    }
}