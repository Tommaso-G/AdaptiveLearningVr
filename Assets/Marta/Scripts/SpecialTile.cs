using UnityEngine;

public abstract class SpecialTile : MonoBehaviour
{
    [SerializeField]
    private bool singleSuport = false;
    [SerializeField]
    private BoxPlayer _boxPlayer;

    public virtual BoxPlayer boxPlayer => _boxPlayer;
    public virtual bool SingleSupport => singleSuport;
    public virtual void TileEffect() { }

}
