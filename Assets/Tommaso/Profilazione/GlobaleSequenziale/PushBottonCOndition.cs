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

        /// <summary>
        /// Solo per XRKnob: la condizione si completa quando il valore supera questa soglia (0–1).
        /// Lascia a 0 per completare su qualsiasi movimento.
        /// </summary>
        [DataMember]
        [DisplayName("Soglia Knob (0–1)")]
        public float KnobThreshold { get; set; } = 0f;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Attendi pressione / interazione XR";

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
        private XRGripButton gripButton;
        private XRKnob knob;
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

            // XRJoystick
            joystick = targetObject.GetComponent<XRJoystick>();
            if (joystick != null)
            {
                joystick.onValueChangeX.AddListener(OnJoystickMoved);
                joystick.onValueChangeY.AddListener(OnJoystickMoved);
                return;
            }

            // XRKnob
            knob = targetObject.GetComponent<XRKnob>();
            if (knob != null)
            {
                knob.onValueChange.AddListener(OnKnobChanged);
                return;
            }

            // XRGripButton
            gripButton = targetObject.GetComponent<XRGripButton>();
            if (gripButton != null)
            {
                gripButton.onPress.AddListener(OnPressed);
                return;
            }

            // XRPushButton (fallback)
            button = targetObject.GetComponent<XRPushButton>();
            if (button != null)
            {
                button.onPress.AddListener(OnPressed);
                return;
            }

            Debug.LogError("[XRButtonPressedCondition] Il GameObject target non contiene " +
                           "XRJoystick, XRKnob, XRGripButton né XRPushButton!");
        }

        // ---- callback ----

        private void OnPressed()
        {
            pressed = true;
        }

        private void OnJoystickMoved(float value)
        {
            if (Mathf.Abs(value) > 0f)
                pressed = true;
        }

        private void OnKnobChanged(float value)
        {
            if (value > Data.KnobThreshold)
                pressed = true;
        }

        // ---- ciclo ----

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
            {
                bool anyComponentPresent = button != null || gripButton != null
                                        || knob != null || joystick != null;

                if (!anyComponentPresent)
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

            if (gripButton != null)
                gripButton.onPress.RemoveListener(OnPressed);

            if (knob != null)
                knob.onValueChange.RemoveListener(OnKnobChanged);

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