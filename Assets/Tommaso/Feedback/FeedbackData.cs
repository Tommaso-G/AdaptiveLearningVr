using UnityEngine;

[CreateAssetMenu(fileName = "FeedbackData", menuName = "VR Feedback/Feedback Data")]
public class FeedbackData : ScriptableObject
{


    [Header("Prefab UI")]
    public GameObject prefabUI; 

    [Header("Numero di step all'interno del capitolo")]
    public int Step;


    public enum AttivoRiflessivo { Attivo, Riflessivo }
    public enum SensitivoIntuitivo { Sensitivo, Intuitivo }
    public enum VisivoVerbale { Visivo, Verbale }
    public enum SequenzialeGlobale { Sequenziale, Globale }

    [Header("Stile di Apprendimento (FSLSM)")]
    public AttivoRiflessivo attivoRiflessivo = AttivoRiflessivo.Attivo;
    public SensitivoIntuitivo sensitivoIntuitivo = SensitivoIntuitivo.Sensitivo;
    public VisivoVerbale visivoVerbale = VisivoVerbale.Visivo;
    public SequenzialeGlobale sequenzialeGlobale = SequenzialeGlobale.Sequenziale;


}