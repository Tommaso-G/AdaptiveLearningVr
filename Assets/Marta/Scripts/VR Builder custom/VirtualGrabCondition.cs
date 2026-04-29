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

[DataContract(IsReference = true)]
public class VirtualGrabCondition : Condition<VirtualGrabCondition.VirtualGrabConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new VirtualGrabConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new VirtualGrabConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Is Virtually Grabbed?")]
    public class VirtualGrabConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public bool isGrabbed = false;
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class VirtualGrabConditionAutocompleter : Autocompleter<VirtualGrabConditionData>
    {
        public VirtualGrabConditionAutocompleter(VirtualGrabConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            Data.isGrabbed = true;
            Debug.Log("Autocompletamento virtual grab");
        }
    }

    public class VirtualGrabConditionActiveProcess : StageProcess<VirtualGrabConditionData>
    {
        public override void Start()
        {

            Data.IsCompleted = false;
            Data.isGrabbed = false;
            if (Data.Target.Value.GameObject.GetComponent<VisualProxy>() == null)
            {
                Data.isGrabbed = true;
                Debug.Log("Visual proxy non trovato");
            }
        }

        public override IEnumerator Update()
        {
            while (!Data.isGrabbed)
            {
                Data.isGrabbed = Data.Target.Value.GameObject.GetComponent<VisualProxy>().isGrabbed;
                yield return null;
            }

            Data.IsCompleted = true;
        }

        public override void End()
        {
        }

        // Nothing to fast-forward.
        // We will explain it soon.
        public override void FastForward()
        {
        }

        // Declare the constructor. It calls the base method to bind the data object with the process.
        public VirtualGrabConditionActiveProcess(VirtualGrabConditionData data) : base(data)
        {
        }
    }

    public VirtualGrabCondition()
    {
        Data.Name = "Virtual Grab";
    }
}