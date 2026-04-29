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
public class RotationCondition : Condition<RotationCondition.RotationConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new RotationConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new RotationConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Is Object rotated?")]
    public class RotationConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        // We will check how far the target from being upside down in degrees.
        // If the difference is lower than threshold, we must complete the condition.
        [DataMember]
        public float xRot { get; set; } = 0f;
        [DataMember]
        public float yRot { get; set; } = 0f;
        [DataMember]
        public float zRot { get; set; } = 0f;

        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class RotationConditionAutocompleter : Autocompleter<RotationConditionData>
    {
        public RotationConditionAutocompleter(RotationConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            // Turn the target upside down, as it would normally happen.
            Data.Target.Value.GameObject.transform.localRotation = Quaternion.Euler(Data.xRot, Data.yRot, Data.zRot);
        }
    }

    public class RotationConditionActiveProcess : StageProcess<RotationConditionData>
    {
        public override void Start()
        {
        }

        public override IEnumerator Update()
        {
            Quaternion targetQuaterion = Quaternion.Euler(Data.xRot, Data.yRot, Data.zRot);
            // Get the difference between vector pointing down,
            // And the vector that comes out of the "roof" of the target.
            // Then compare it with the threshold from data.
            while (Quaternion.Angle(Data.Target.Value.GameObject.transform.localRotation, targetQuaterion) > 10f)
            {
                //If the angle is more than the threshold, wait for the next frame.
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
        public RotationConditionActiveProcess(RotationConditionData data) : base(data)
        {
        }
    }

    public RotationCondition()
    {
        Data.Name = "Rotation Target";
    }
}