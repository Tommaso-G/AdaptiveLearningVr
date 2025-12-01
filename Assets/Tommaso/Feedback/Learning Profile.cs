using UnityEngine;

public class LearningProfile : MonoBehaviour
{
    [Header("Stile di Apprendimento (FSLSM)")]

    public LearningEnums.AttivoRiflessivo attivoRiflessivo = LearningEnums.AttivoRiflessivo.Attivo;
    public LearningEnums.SensitivoIntuitivo sensitivoIntuitivo = LearningEnums.SensitivoIntuitivo.Sensitivo;
    public LearningEnums.VisivoVerbale visivoVerbale = LearningEnums.VisivoVerbale.Visivo;
    public LearningEnums.SequenzialeGlobale sequenzialeGlobale = LearningEnums.SequenzialeGlobale.Sequenziale;

    [Header("Descrizione")]
    [SerializeField, TextArea(2, 3)]
    private string descrizioneCorrente;

    private void OnValidate()
    {
        descrizioneCorrente =
            $"Profilo attuale:\n" +
            $"{attivoRiflessivo}, " +
            $"{sensitivoIntuitivo}, " +
            $"{visivoVerbale}, " +
            $"{sequenzialeGlobale}";
    }
}
