using UnityEngine;

public class ResetTimeScale : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;
        Debug.Log("[ResetTimeScale] Time.timeScale impostato a 1 all'avvio della scena.");
    }
}
