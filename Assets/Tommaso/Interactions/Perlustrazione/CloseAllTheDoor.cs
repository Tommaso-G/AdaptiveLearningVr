using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class CheckAllDoorsClosed : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Header("Porte da controllare")]
    public List<ClosableDoor> closableDoors = new List<ClosableDoor>();
    public List<ExitDoor> exitDoors = new List<ExitDoor>();

    [Header("Error Reporter")]
    public ErrorReporter ErrorReporter;

    public void Check()
    {
        if (IsCompleted) return;

        List<string> openDoors = new List<string>();

        foreach (ClosableDoor door in closableDoors)
        {
            if (door == null) continue;
            if (!door.IsClosed)
                openDoors.Add(door.gameObject.name);
        }

        foreach (ExitDoor door in exitDoors)
        {
            if (door == null) continue;
            if (!door.isDoorClosed)
                openDoors.Add(door.gameObject.name);
        }

        if (openDoors.Count > 0)
        {
            if (ErrorReporter != null)
            {
                ErrorReporter.RegisterError("VerificaPorte");
                Debug.Log("ERRORE REGISTRATO");
            }
            else
            {
                Debug.LogError("[CheckAllDoorsClosed] ErrorReporter non assegnato!");
            }

            Debug.LogWarning($"[CheckAllDoorsClosed] Porte rimaste aperte: {string.Join(", ", openDoors)}");
        }
        else
        {
            Debug.Log("[CheckAllDoorsClosed] Tutte le porte sono chiuse.");
        }



        IsCompleted = true;
    }
}