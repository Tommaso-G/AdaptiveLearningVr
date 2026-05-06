using UnityEngine;

public class VideoPlayerController : MonoBehaviour
{
    private UnityEngine.Video.VideoPlayer videoPlayer;

    void OnEnable()
    {
        videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("❌ Nessun VideoPlayer trovato su questo oggetto");
            return;
        }

        videoPlayer.Stop();
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
        Debug.Log("⏹️ Stop");
    }

    public void Pause()
    {
        if (videoPlayer == null) return;
        videoPlayer.Pause();
        Debug.Log("⏸️ Pausa");
    }

    public void Forward5()
    {
        if (videoPlayer == null) return;

        double newTime = videoPlayer.time + 5.0;
        double duration = (double)videoPlayer.frameCount / videoPlayer.frameRate;

        videoPlayer.time = Mathf.Min((float)newTime, (float)duration);
        Debug.Log($"⏩ Avanti → {videoPlayer.time:F2}s");
    }

    public void Backward5()
    {
        if (videoPlayer == null) return;

        double newTime = videoPlayer.time - 5.0;
        videoPlayer.time = Mathf.Max((float)newTime, 0f);
        Debug.Log($"⏪ Indietro → {videoPlayer.time:F2}s");
    }
}