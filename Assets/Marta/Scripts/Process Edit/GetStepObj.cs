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
public class GetStepObj : Condition<GetStepObj.GetStepObjData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new GetStepObjActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new GetStepObjAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Get the object reference")]
    public class GetStepObjData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        [DisplayName("Step Object")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();


        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class GetStepObjAutocompleter : Autocompleter<GetStepObjData>
    {
        public GetStepObjAutocompleter(GetStepObjData data) : base(data)
        {
        }

        public override void Complete()
        {
        }
    }

    public class GetStepObjActiveProcess : StageProcess<GetStepObjData>
    {
        public override void Start()
        {
        }

        public override IEnumerator Update()
        {
            while (false)
            { 
                yield return null;
            }

            // If the angle is less or equal to threshold, mark the condition as complete.
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
        public GetStepObjActiveProcess(GetStepObjData data) : base(data)
        {
        }
    }

    public GetStepObj()
    {
        Data.Name = "Get object reference";
    }
}