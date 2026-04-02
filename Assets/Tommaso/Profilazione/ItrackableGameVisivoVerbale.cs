using System.Collections.Generic;

public interface ITrackableGameVisivoVerbale
{
    string GameID { get; }        
    event System.Action<RoundData> OnRoundFinished;

}

[System.Serializable]
public class RoundData
{
    public string gameID;
    public ModalitaGioco modalita;      // "Immagini" o "Parole"
    public int numeroRound;
    public int errori;
    public float tempoSecondi;

    public Dictionary<string, float> parametriExtra = new Dictionary<string, float>();
}

public enum ModalitaGioco
{
    Visivo,
    Verbale
}