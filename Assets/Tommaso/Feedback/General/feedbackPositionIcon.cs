using UnityEngine;

public class feedbackPositionIcon : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // Il file deve stare in Assets/Gizmos/nome_icona.png
        Gizmos.DrawIcon(transform.position, "feedbackPositionIcon.png", true);
    }
}