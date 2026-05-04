using UnityEngine;

public class HandMenuPanel : MonoBehaviour
{
    [SerializeField] private string menuId;
    [SerializeField] private Animator animator;

    public string MenuId => menuId;

    public void Open()
    {
        gameObject.SetActive(true);
        animator.SetBool("FadeOut", false);
        animator.SetBool("FadeIn", true);
    }

    public void Close()
    {
        animator.SetBool("FadeIn", false);
        animator.SetBool("FadeOut", true);
    }
}
