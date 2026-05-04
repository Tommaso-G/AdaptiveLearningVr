using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
[DisplayName("XR Button Pressed")]
public class XRButtonPressedCondition : Condition<XRButtonPressedCondition.XRButtonPressedData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new XRButtonPressedActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new XRButtonPressedAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    public class XRButtonPressedData : IConditionData
    {
        [DataMember]
        [DisplayName("Bottone Target")]
        public SingleSceneObjectReference ButtonTarget { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Attendi pressione bottone XR";

        [IgnoreDataMember]
        public bool IsCompleted { get; set; }
    }

    // ============================
    // AUTO-COMPLETER
    // ============================
    public class XRButtonPressedAutocompleter : Autocompleter<XRButtonPressedData>
    {
        public XRButtonPressedAutocompleter(XRButtonPressedData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
public class XRButtonPressedActiveProcess : StageProcess<XRButtonPressedData>
{
    private XRPushButton button;
    private XRJoystick joystick;
    private bool pressed = false;

    public XRButtonPressedActiveProcess(XRButtonPressedData data) : base(data) { }

    public override void Start()
    {
        var targetObject = Data.ButtonTarget.Value?.GameObject;
        if (targetObject == null)
        {
            Debug.LogError("[XRButtonPressedCondition] Nessun target assegnato!");
            return;
        }

        // Prova prima con XRJoystick
        joystick = targetObject.GetComponent<XRJoystick>();
        if (joystick != null)
        {
            joystick.onValueChangeX.AddListener(OnJoystickMoved);
            joystick.onValueChangeY.AddListener(OnJoystickMoved);
            return;
        }

        // Fallback su XRPushButton
        button = targetObject.GetComponent<XRPushButton>();
        if (button != null)
        {
            button.onPress.AddListener(OnPressed);
            return;
        }

        Debug.LogError("[XRButtonPressedCondition] Il GameObject target non contiene XRJoystick né XRPushButton!");
    }

    private void OnPressed()
    {
        pressed = true;
    }

    private void OnJoystickMoved(float value)
    {
        if (Mathf.Abs(value) > 0f)
            pressed = true;
    }

    public override IEnumerator Update()
    {
        while (!Data.IsCompleted)
        {
            if (button == null && joystick == null)
            {
                Debug.LogWarning("[XRButtonPressedCondition] Target distrutto o rimosso dalla scena.");
                yield break;
            }

            if (pressed)
                Data.IsCompleted = true;

            yield return null;
        }
    }

    public override void End()
    {
        if (button != null)
            button.onPress.RemoveListener(OnPressed);

        if (joystick != null)
        {
            joystick.onValueChangeX.RemoveListener(OnJoystickMoved);
            joystick.onValueChangeY.RemoveListener(OnJoystickMoved);
        }
    }

    public override void FastForward()
    {
        Data.IsCompleted = true;
    }
}

}
