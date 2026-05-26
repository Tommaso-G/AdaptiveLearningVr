using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core.Properties;
public class UIButtonInteractionSource : InteractionSource
{
    private Button buttonUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override bool CanHandle(GameObject obj)
    => obj.TryGetComponent<Button>(out _);

    private void Awake()
    {
        buttonUI = GetComponent<Button>();
    }

    private void OnEnable()
    {
        buttonUI.onClick.AddListener(OnButtonUIClicked);
    }

    private void OnDisable()
    {
        buttonUI.onClick.AddListener(OnButtonUIClicked);
    }
    private void OnButtonUIClicked()
    {
        RaiseInteraction(
            InteractionKind.UIButton,
            gameObject
        );
    }
}
