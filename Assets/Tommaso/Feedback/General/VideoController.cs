using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("❌ Nessun VideoPlayer trovato");
            return;
        }

        videoPlayer.playOnAwake = false;

        // Callback quando il video è pronto
        videoPlayer.prepareCompleted += OnVideoPrepared;

        // Prepara il video senza farlo partire
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Vai al primo frame
        vp.frame = 0;

        // Mostra il frame senza playback
        vp.Pause();

        Debug.Log("✅ Primo frame mostrato");
    }

    public void Play()
    {
        if (videoPlayer == null) return;

        videoPlayer.Play();
        Debug.Log("▶️ Play");
    }

    public void Stop()
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();

        // Torna al primo frame
        videoPlayer.frame = 0;
        videoPlayer.Pause();

        Debug.Log("⏹️ Stop");
    }

    public void Pause()
    {
        if (videoPlayer == null) return;

        videoPlayer.Pause();
        Debug.Log("⏸️ Pausa");
    }
}