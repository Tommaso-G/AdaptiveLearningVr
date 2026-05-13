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
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.frame = 0;
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

    public void AvanzaDi5Secondi()
    {
        if (videoPlayer == null) return;

        double nuovoTempo = videoPlayer.time + 5.0;
        double durata = videoPlayer.length;

        videoPlayer.time = nuovoTempo >= durata ? durata - 0.001 : nuovoTempo;

        Debug.Log($"⏩ +5s → {videoPlayer.time:F1}s");
    }

    public void TornaIndietro5Secondi()
    {
        if (videoPlayer == null) return;

        double nuovoTempo = videoPlayer.time - 5.0;

        videoPlayer.time = nuovoTempo <= 0.0 ? 0.0 : nuovoTempo;

        Debug.Log($"⏪ -5s → {videoPlayer.time:F1}s");
    }
}