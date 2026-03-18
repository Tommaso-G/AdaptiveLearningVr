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
}

public enum ModalitaGioco
{
    Visivo,
    Verbale
}