using UnityEngine;

public class EscapeRoutesManager : MonoBehaviour
{
    [SerializeField] private ExitDoor[] escapeRoutes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        escapeRoutes = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);
    }

    public void DeleselectAll()
    {
        foreach (var door in escapeRoutes)
        {
            door.Deselect();
        }
    }
}
