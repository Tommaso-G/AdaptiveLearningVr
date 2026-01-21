using UnityEngine;

public class HandMenuRequester : MonoBehaviour
{
    [SerializeField] private HandMenu menuManager;
    [SerializeField] private string menuId;

    public void OpenMenu()
    {
        print("OPEN REQUEST FROM REQUESTER");
        menuManager.RequestOpen(menuId);
    }

    public void CloseMenu()
    {
        print("CLOSE REQUEST FROM REQUESTER");
        menuManager.RequestClose(menuId);
    }
}
