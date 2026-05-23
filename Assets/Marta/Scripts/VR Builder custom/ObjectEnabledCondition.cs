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
public class ObjectEnabledCondition : Condition<ObjectEnabledCondition.ObjectEnabledConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ObjectEnabledConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ObjectEnabledConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Has the object been Enabled?")]
    public class ObjectEnabledConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        [DisplayName("Object to enable")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ObjectEnabledConditionAutocompleter : Autocompleter<ObjectEnabledConditionData>
    {
        public ObjectEnabledConditionAutocompleter(ObjectEnabledConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            GameObject obj = Data.Target.Value.GameObject;
            obj.SetActive(false);
        }
    }

    public class ObjectEnabledConditionActiveProcess : StageProcess<ObjectEnabledConditionData>
    {
        public override void Start()
        {
            Data.IsCompleted = false;
        }

        public override IEnumerator Update()
        {
            while (Data.Target.Value.GameObject.activeSelf == false)
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
        public ObjectEnabledConditionActiveProcess(ObjectEnabledConditionData data) : base(data)
        {
        }
    }

    public ObjectEnabledCondition()
    {
        Data.Name = "Object Enabled";
    }
}