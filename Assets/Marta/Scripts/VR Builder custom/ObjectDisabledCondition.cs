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
public class ObjectDisabledCondition : Condition<ObjectDisabledCondition.ObjectDisabledConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ObjectDisabledConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ObjectDisabledConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Has the object been disabled?")]
    public class ObjectDisabledConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        [DisplayName("Object to disable")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ObjectDisabledConditionAutocompleter : Autocompleter<ObjectDisabledConditionData>
    {
        public ObjectDisabledConditionAutocompleter(ObjectDisabledConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            GameObject obj = Data.Target.Value.GameObject;
            obj.SetActive(false);
        }
    }

    public class ObjectDisabledConditionActiveProcess : StageProcess<ObjectDisabledConditionData>
    {
        public override void Start()
        {
            Data.IsCompleted = false;
        }

        public override IEnumerator Update()
        {
            while (Data.Target.Value.GameObject.activeSelf == true)
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
        public ObjectDisabledConditionActiveProcess(ObjectDisabledConditionData data) : base(data)
        {
        }
    }

    public ObjectDisabledCondition()
    {
        Data.Name = "Object Disabled";
    }
}