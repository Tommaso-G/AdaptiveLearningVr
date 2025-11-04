using System;
using System.Collections;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using VRBuilder.Core.Conditions;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using UnityEngine.UI;

[DataContract(IsReference = true)]
public class ButtonSelectedCondition : Condition<ButtonSelectedCondition.ButtonSelectedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ButtonSelectedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ButtonSelectedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Is Button selected?")]
    public class ButtonSelectedConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public bool clicked = false;
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ButtonSelectedConditionAutocompleter : Autocompleter<ButtonSelectedConditionData>
    {
        public ButtonSelectedConditionAutocompleter(ButtonSelectedConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            Data.clicked = true;
        }
    }

    public class ButtonSelectedConditionActiveProcess : StageProcess<ButtonSelectedConditionData>
    {
        public override void Start()
        {
            Button button = Data.Target.Value.GameObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }

        }

        public override IEnumerator Update()
        {
            // Get the difference between vector pointing down,
            // And the vector that comes out of the "roof" of the target.
            // Then compare it with the threshold from data.
            while (Data.clicked == false)
            {
                //If the angle is more than the threshold, wait for the next frame.
                yield return null;
            }

            // If the angle is less or equal to threshold, mark the condition as complete.
            Data.IsCompleted = true;
        }

        void OnButtonClicked()
        {
            Data.clicked = true;
        }


        public override void End()
        {
            Button button = Data.Target.Value.GameObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        // Nothing to fast-forward.
        // We will explain it soon.
        public override void FastForward()
        {
        }

        // Declare the constructor. It calls the base method to bind the data object with the process.
        public ButtonSelectedConditionActiveProcess(ButtonSelectedConditionData data) : base(data)
        {
        }
    }

    public ButtonSelectedCondition()
    {
        Data.Name = "Button Selected";
    }
}