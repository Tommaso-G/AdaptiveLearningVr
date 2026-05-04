using UnityEngine;
using System.Collections;
public class FireHit : MonoBehaviour
{
    [SerializeField]
    private float FireHealt = 100f;
    [SerializeField]
    private float damage = 1f;

    public void Extinguish()
    {
        FireHealt -= damage;
        //print("Fire " + gameObject.GetInstanceID() + " healt: " + FireHealt);
        if (FireHealt <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
