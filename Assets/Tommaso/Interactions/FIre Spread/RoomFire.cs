using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class RoomFire : MonoBehaviour
{
    public ClosableDoor Door;
    public GameObject Fire;

    public bool activateProximitySpawenr = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (activateProximitySpawenr)
            {
                Fire.GetComponent<ProximitySpawner>().enabled = true;
            }
        }
    }

    public void actvateProximitySpawnerFunction()
    {
        activateProximitySpawenr = true;
    }
    
}
