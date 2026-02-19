using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class SlidingTilesMgr : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; }
    private List<SlidingTile> slidingTiles = new List<SlidingTile>();
    private int onTarget = 0;
    [SerializeField] private List<MySocket> XRSockets = new List<MySocket>();
    private int formOnTarget = 0;
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

            foreach (SlidingTile tile in slidingTiles)
            {
                tile.onTarget = true;
            }

            IsCompleted = true;
        }
        else
        {
            onTarget = 0;
        }
    }

    //public void FormOnTarget()
    //{
    //    foreach (MySocket socket in XRSockets)
    //    {
    //        if (socket.filled)
    //        {
    //            formOnTarget += 1;
    //        }
    //    }

    //    if (formOnTarget == slidingTiles.Count)
    //    {
    //        IsCompleted = true;
    //    }
    //    else
    //    {
    //        formOnTarget = 0;
    //    }
    //}
}
