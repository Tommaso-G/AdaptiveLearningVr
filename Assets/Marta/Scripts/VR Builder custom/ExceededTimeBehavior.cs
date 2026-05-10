using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;
using System;
using System.Reflection;

[DataContract(IsReference = true)]
public class ExceededTimeBehavior : Behavior<ExceededTimeBehavior.EntityData>
{

    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("Capitolo a cui assegnare l'errore")]
        [DataMember]
        [ChapterDropdown]
        public string chapterErrorName;

        [DataMember]
        public bool showErrorOnUI = true;

        [DisplayName("Oggetto con StepErrorTracker")]
        [DataMember]
        public SingleSceneObjectReference errorTrackerObj { get; set; } = new SingleSceneObjectReference();

        [DisplayName("Oggetto con ExecuteChapterController")]
        [DataMember]
        public SingleSceneObjectReference executionOrderControllerObj { get; set; } = new SingleSceneObjectReference();


        [DisplayName("Oggetto con ChapterSkipHandler")]
        [DataMember]
        public SingleSceneObjectReference chapterSkipHandlerObj { get; set; } = new SingleSceneObjectReference();


        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Exceeded Chapter Timer";
    }

    public override IStageProcess GetActivatingProcess()
    {
        return new ExceededTimeBehaviorProcess(Data);
    }

    // --------------------------------------------------------------
    private class ExceededTimeBehaviorProcess : StageProcess<EntityData>
    {
        public ExceededTimeBehaviorProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            ErrorReporter errorReporter = new ErrorReporter();
            StepErrorTracker errorTracker = Data.errorTrackerObj?.Value.GameObject.GetComponent<StepErrorTracker>();
            ExecutionOrderController executionOrderController = Data.executionOrderControllerObj?.Value.GameObject.GetComponent<ExecutionOrderController>();
            ChapterSkipHandler chapterSkipHandler = Data.chapterSkipHandlerObj?.Value.GameObject.GetComponent<ChapterSkipHandler>();

            if (errorTracker == null || executionOrderController == null || chapterSkipHandler == null)
            {
                Debug.Log("[ExeededTimeBehavior] mancano i componenti necessari");
                return;
            }


            if (Data.showErrorOnUI)
            {
                errorReporter.setReference(errorTracker, executionOrderController);
                if (Data.chapterErrorName == null)
                {
                    Data.chapterErrorName = "Unknown Chapter";
                }

                errorReporter.chapterErrorName = Data.chapterErrorName;
                errorReporter.RegisterError("ChapterTimer");
            }

            chapterSkipHandler.NotifyChapterSkipped(Data.chapterErrorName);
        }

        public override IEnumerator Update()
        {
            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }
    }
}