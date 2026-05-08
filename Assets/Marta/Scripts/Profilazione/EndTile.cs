using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EndTile : SpecialTile, ICompletableStep
{
    public bool IsCompleted { get; private set; }
    [SerializeField]
    AudioSource audio;
    [SerializeField]
    VisualEffect effect;
    [SerializeField]
    Transform winPosition;

    public MinigameDataSender dataSender;

    private float timeOnWinPosition = 0f;
    private bool isWinning = false;

    public override void TileEffect()
    {
        if (!SingleSupport)
        {
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(Win());
            return;
        }
        if (boxPlayer.isDownRL || boxPlayer.isDownUD)
        {
            timeOnWinPosition = 0f;
            return;
        }

        Debug.Log($"[EndTile] BoxPlayer position: {boxPlayer.transform.position}, WinPosition: {winPosition?.position}, Diff X: {Mathf.Abs(boxPlayer.transform.position.x - winPosition?.position.x ?? 0)}, Diff Z: {Mathf.Abs(boxPlayer.transform.position.z - winPosition?.position.z ?? 0)}");

        if (!IsBoxAtWinPosition())
        {
            timeOnWinPosition = 0f;
            return;
        }

        timeOnWinPosition += Time.deltaTime;
        Debug.Log($"[EndTile] Cubo in posizione di vittoria da: {timeOnWinPosition:F2}s");

        if (timeOnWinPosition >= 0.5f && !isWinning)
        {
            isWinning = true;
            BoxCollider box = boxPlayer.GetComponent<BoxCollider>();
            box.size = new Vector3(0.9f, 1.9f, 0.9f);
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(Win());
        }
    }

    private bool IsBoxAtWinPosition()
    {
        if (winPosition == null)
        {
            Debug.LogWarning("[EndTile] winPosition non assegnato!");
            return false;
        }

        float threshold = 0.3f;
        Vector3 boxPos = boxPlayer.transform.position;
        Vector3 targetPos = winPosition.position;

        return Mathf.Abs(boxPos.x - targetPos.x) < threshold &&
               Mathf.Abs(boxPos.z - targetPos.z) < threshold;
    }

    private IEnumerator Win()
    {
        yield return new WaitForSeconds(1);
        audio.Play();
        effect.enabled = true;
        boxPlayer.gameObject.SetActive(false);
        dataSender?.Complete();
        IsCompleted = true;
    }
}