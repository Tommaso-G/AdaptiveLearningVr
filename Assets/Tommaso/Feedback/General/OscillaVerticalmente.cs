using UnityEngine;

public class OscillaVerticalmente : MonoBehaviour
{
    public float ampiezza = 1f;
    public float velocita = 1f;

    private Vector3 posizioneInizialeLocale;

    void Start()
    {
        posizioneInizialeLocale = transform.localPosition;
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * velocita) * ampiezza;

        transform.localPosition = new Vector3(
            posizioneInizialeLocale.x,
            posizioneInizialeLocale.y + offsetY,
            posizioneInizialeLocale.z
        );
    }
}