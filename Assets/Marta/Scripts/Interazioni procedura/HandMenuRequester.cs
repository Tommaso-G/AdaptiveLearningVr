using UnityEngine;

public class HandMenuRequester : MonoBehaviour
{
    [SerializeField] private HandMenu menuManager;
    [SerializeField] private string menuId;

    public void OpenMenu()
    {
        print("OPEN REQUEST FROM REQUESTER with " + menuId);
        menuManager.RequestOpen(menuId, this);
    }

    public void CloseMenu()
    {
        print("CLOSE REQUEST FROM REQUESTER");
        menuManager.RequestClose(menuId);
    }
}
