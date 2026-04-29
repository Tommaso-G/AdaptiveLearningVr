using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class FallTile : SpecialTile
{
    [SerializeField]
    private List<BridgeTile> BridgeTiles = new List<BridgeTile>();
    public override void TileEffect()
    {
        if (!SingleSupport)
        {
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(RestartGame());
        }
        if (boxPlayer.isDownRL || boxPlayer.isDownUD)
        {
            return;
        }
        else
        {
            boxPlayer.GetComponent<Rigidbody>().isKinematic = false;
            StartCoroutine(RestartGame());
        }
    }
    private IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(2f);
        foreach(BridgeTile b in BridgeTiles)
        {
            b.Reset();
        }
        boxPlayer.Reset();
        yield return null;
    }
}
