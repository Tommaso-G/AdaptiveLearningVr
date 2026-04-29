using System.Collections.Generic;
using UnityEngine;

public class BridgeTile : SpecialTile
{
    [SerializeField]
    private List<Animator> bridgeAnim;
    [SerializeField]
    private List<FallTile> fallTiles;

    public void Reset()
    {
        foreach (Animator a in bridgeAnim)
        {
            a.SetBool("open", false);
        }

        foreach (FallTile f in fallTiles)
        {
            f.gameObject.SetActive(true);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            TileEffect();
        }
    }

    public override void TileEffect()
    {
        print("Effect Called from bridge: " + SingleSupport);
        bool open = false;
        if (!SingleSupport)
        {
            foreach (Animator a in bridgeAnim)
            {
                open = a.GetBool("open");
                a.SetBool("open", !open);
            }

            foreach (FallTile f in fallTiles)
            {
                f.gameObject.SetActive(open);
            }
        }
        else
        {
            if (boxPlayer.isDownRL || boxPlayer.isDownUD)
            {
                return;
            }
            else
            {
                foreach (Animator a in bridgeAnim)
                {
                    open = a.GetBool("open");
                    a.SetBool("open", !open);
                }

                foreach (FallTile f in fallTiles)
                {
                    f.gameObject.SetActive(open);
                }
            }
        }
    }
}
