using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EndTile : SpecialTile
{
    [SerializeField]
    AudioSource audio;
    [SerializeField]
    VisualEffect effect;
    public override void TileEffect()
    {
        if (!SingleSupport)
        {
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(Win());
        }
        if (boxPlayer.isDownRL || boxPlayer.isDownUD)
        {
            return;
        }
        else
        {
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
    }
}
