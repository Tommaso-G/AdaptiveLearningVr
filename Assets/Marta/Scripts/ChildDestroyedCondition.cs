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
using static Unity.VisualScripting.Metadata;

[DataContract(IsReference = true)]
public class ChildDestroyedCondition : Condition<ChildDestroyedCondition.ChildDestroyedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Always return a new instance.
        return new ChildDestroyedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Always return a new instance.
        return new ChildDestroyedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Did the children count change?")]
    public class ChildDestroyedConditionData : IConditionData
    {
        // A reference to the target object that we will check.
        [DataMember]
        [DisplayName("Parent Object")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Transitioning when obj has no children")]
        public bool noChildren = false;

        public int children = 0;
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ChildDestroyedConditionAutocompleter : Autocompleter<ChildDestroyedConditionData>
    {
        public ChildDestroyedConditionAutocompleter(ChildDestroyedConditionData data) : base(data)
        {
        }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    public class ChildDestroyedConditionActiveProcess : StageProcess<ChildDestroyedConditionData>
    {
        public override void Start()
        {
            Data.IsCompleted = false;
            Data.children = Data.Target.Value.GameObject.transform.childCount;
            Debug.Log("Children: " + Data.children);
        }

        public override IEnumerator Update()
        {
            int currentChildren = Data.Target.Value.GameObject.transform.childCount;
            if (Data.noChildren)
            {
                while (currentChildren > 0)
                {
                    currentChildren = Data.Target.Value.GameObject.transform.childCount;
                    yield return null;
                }

                Data.IsCompleted = true;
            }
            else
            {
                while (Data.children == currentChildren)
                {
                    currentChildren = Data.Target.Value.GameObject.transform.childCount;
                    yield return null;
                }

                if (currentChildren != 0)
                {
                    Data.IsCompleted = true;
                }
            }

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
        public ChildDestroyedConditionActiveProcess(ChildDestroyedConditionData data) : base(data)
        {
        }
    }

    public ChildDestroyedCondition()
    {
        Data.Name = "Child Destroyed";
    }
}