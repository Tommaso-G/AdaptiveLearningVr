
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class GetFeedbackPosition : Behavior<GetFeedbackPosition.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("Posizione (Scene Object)")]
        [DataMember]
        public SingleSceneObjectReference feedbackPosition { get; set; } = new SingleSceneObjectReference();


        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Adaptive Feedback Manager";
    }

    public override IStageProcess GetActivatingProcess() => new FeedbackManagerProcess(Data);

    // --------------------------------------------------------------
    private class FeedbackManagerProcess : StageProcess<EntityData>
    {
        public FeedbackManagerProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            
        }

        public override IEnumerator Update()
        {
            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }


        }


    }

