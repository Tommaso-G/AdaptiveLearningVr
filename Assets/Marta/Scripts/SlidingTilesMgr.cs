using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class SlidingTilesMgr : MonoBehaviour
{
    private List<SlidingTile> slidingTiles = new List<SlidingTile>();
    private int onTarget = 0;
    [SerializeField] private List<MySocket> XRSockets = new List<MySocket>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slidingTiles = GetComponentsInChildren<SlidingTile>().ToList();
        XRSockets = GetComponentsInChildren<MySocket>(true).ToList();
    }

    public void TileOnTarget()
    {

        foreach (SlidingTile tile in slidingTiles)
        {
            if (tile.on)
            {
                onTarget += 1;
            }
        }

        if (onTarget == slidingTiles.Count)
        {
            foreach (MySocket socket in XRSockets)
            {
                socket.gameObject.SetActive(true);
            }

            foreach(SlidingTile tile in slidingTiles)
            {
                tile.onTarget = true;
            }
        }
        else
        {
            onTarget = 0;
        }
    }
}
