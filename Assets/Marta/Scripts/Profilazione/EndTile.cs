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

    public MinigameDataSender dataSender;

    public override void TileEffect()
    {
        if (!SingleSupport)
        {
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(Win());
            return; // <- aggiunto
        }
        if (boxPlayer.isDownRL || boxPlayer.isDownUD)
        {
            return;
        }
        else
        {
            BoxCollider box = boxPlayer.GetComponent<BoxCollider>();
            box.size = new Vector3(0.9f, 1.9f, 0.9f);
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(Win());
        }
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
