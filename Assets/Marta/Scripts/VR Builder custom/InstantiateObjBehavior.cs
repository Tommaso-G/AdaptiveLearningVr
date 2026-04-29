
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class InstantiateObjBehavior : Behavior<InstantiateObjBehavior.EntityData>
{

    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("Objects Spawner(Scene Object)")]
        [DataMember]
        public SingleSceneObjectReference obstaclesSpawner { get; set; } = new SingleSceneObjectReference();


        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Instantiate Objects";
    }

    public override IStageProcess GetActivatingProcess()
    {
        return new InstantiateObjectsProcess(Data);
    }

    // --------------------------------------------------------------
    private class InstantiateObjectsProcess : StageProcess<EntityData>
    {
        public InstantiateObjectsProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            var go = Data.obstaclesSpawner?.Value.GameObject;

            if (go == null)
            {
                Debug.LogError("Target non assegnato!");
                return;
            }

            ObstaclesSpawner obSpawner = go.GetComponent<ObstaclesSpawner>();
            if (obSpawner == null)
            {
                Debug.LogError($"{go.name} deve avere MyRequiredComponent!");
                return;
            }

            obSpawner.initializeSpawn();
        }

        public override IEnumerator Update()
        {
            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }


    }


}

