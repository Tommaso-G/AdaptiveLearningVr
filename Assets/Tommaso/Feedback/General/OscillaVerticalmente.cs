using UnityEngine;

public class OscillaVerticalmente : MonoBehaviour
{
    [Header("Movimento")]
    public float ampiezza = 1f;     // Quanto si sposta in alto e in basso
    public float velocita = 1f;    // Velocità dell'oscillazione

    private Vector3 posizioneIniziale;

    void Start()
    {
        posizioneIniziale = transform.position;
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * velocita) * ampiezza;

        transform.position = new Vector3(
            posizioneIniziale.x,
            posizioneIniziale.y + offsetY,
            posizioneIniziale.z
        );
    }
}