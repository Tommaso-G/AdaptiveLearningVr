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
using System.Linq;

[DataContract(IsReference = true)]
public class ObjectDeletedCondition : Condition<ObjectDeletedCondition.ObjectDeletedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ObjectDeletedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ObjectDeletedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Has the object been deteled?")]
    public class ObjectDeletedConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        [DisplayName("Object to delete")]
        public MultipleSceneObjectReference Targets { get; set; } = new MultipleSceneObjectReference();


        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ObjectDeletedConditionAutocompleter : Autocompleter<ObjectDeletedConditionData>
    {
        public ObjectDeletedConditionAutocompleter(ObjectDeletedConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            // Turn the target upside down, as it would normally happen.
            foreach (ISceneObject target in Data.Targets.Values)
            {
                GameObject obj = target.GameObject;
                UnityEngine.Object.Destroy(obj);
            }
        }
    }

    public class ObjectDeletedConditionActiveProcess : StageProcess<ObjectDeletedConditionData>
    {
        public override void Start()
        {
        }

        public override IEnumerator Update()
        {
            // Get the difference between vector pointing down,
            // And the vector that comes out of the "roof" of the target.
            // Then compare it with the threshold from data.
            while (Data.Targets.Values.Any(obj => obj?.GameObject != null))
            {
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
        public ObjectDeletedConditionActiveProcess(ObjectDeletedConditionData data) : base(data)
        {
        }
    }

    public ObjectDeletedCondition()
    {
        Data.Name = "Object Deleted";
    }
}