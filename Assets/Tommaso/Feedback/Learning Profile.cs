using UnityEngine;

public class LearningProfile : MonoBehaviour
{
    public enum AttivoRiflessivo { Attivo, Riflessivo }
    public enum SensitivoIntuitivo { Sensitivo, Intuitivo }
    public enum VisivoVerbale { Visivo, Verbale }
    public enum SequenzialeGlobale { Sequenziale, Globale }

    [Header("Stile di Apprendimento (FSLSM)")]

    [Tooltip("Attivo = pratico; Riflessivo = teorico")]
    public AttivoRiflessivo attivoRiflessivo = AttivoRiflessivo.Attivo;

    [Tooltip("Sensitivo = realistico/diegetico; Intuitivo = concettuale/astratto")]
    public SensitivoIntuitivo sensitivoIntuitivo = SensitivoIntuitivo.Sensitivo;

    [Tooltip("Visivo = immagini/schemi; Verbale = testo/voce")]
    public VisivoVerbale visivoVerbale = VisivoVerbale.Visivo;

    [Tooltip("Sequenziale = passo-passo; Globale = panoramico")]
    public SequenzialeGlobale sequenzialeGlobale = SequenzialeGlobale.Sequenziale;


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