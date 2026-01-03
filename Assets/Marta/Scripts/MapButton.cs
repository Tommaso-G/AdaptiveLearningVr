using UnityEngine;

public class MapButton : MonoBehaviour
{
    [field: SerializeField]
    public ExitDoor ExitDoor { get; private set; }

    [field: SerializeField]
    public bool selectableDoor { get; private set; }

    [field: SerializeField]
    public Transform obstacleSpawnArea { get; private set; }


}
