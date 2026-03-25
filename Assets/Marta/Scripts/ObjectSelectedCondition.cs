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
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

[DataContract(IsReference = true)]
public class ObjectSelectedCondition : Condition<ObjectSelectedCondition.ObjectSelectedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ObjectSelectedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ObjectSelectedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Is Object selected?")]
    public class ObjectSelectedConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public bool selected = false;
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ObjectSelectedConditionAutocompleter : Autocompleter<ObjectSelectedConditionData>
    {
        public ObjectSelectedConditionAutocompleter(ObjectSelectedConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            Data.selected = true;
        }
    }

    public class ObjectSelectedConditionActiveProcess : StageProcess<ObjectSelectedConditionData>
    {
        public override void Start()
        {
            XRBaseInteractable interactable = Data.Target.Value.GameObject.GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelected);
            }

        }

        public override IEnumerator Update()
        {
            // Get the difference between vector pointing down,
            // And the vector that comes out of the "roof" of the target.
            // Then compare it with the threshold from data.
            while (Data.selected == false)
            {
                //If the angle is more than the threshold, wait for the next frame.
                yield return null;
            }

            // If the angle is less or equal to threshold, mark the condition as complete.
            Data.IsCompleted = true;
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            Data.selected = true;
        }


        public override void End()
        {
            XRBaseInteractable interactable = Data.Target.Value.GameObject.GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelected);
            }
        }

        // Nothing to fast-forward.
        // We will explain it soon.
        public override void FastForward()
        {
        }

        // Declare the constructor. It calls the base method to bind the data object with the process.
        public ObjectSelectedConditionActiveProcess(ObjectSelectedConditionData data) : base(data)
        {
        }
    }

    public ObjectSelectedCondition()
    {
        Data.Name = "Object Selected";
    }
}