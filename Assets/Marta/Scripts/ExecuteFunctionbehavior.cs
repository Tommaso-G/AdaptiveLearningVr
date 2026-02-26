
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
using System;
using System.Reflection;

[DataContract(IsReference = true)]
public class ExecuteFuntionBehavior : Behavior<ExecuteFuntionBehavior.EntityData>
{

    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("GameObjct with function to execute")]
        [DataMember]
        public SingleSceneObjectReference sceneObj { get; set; } = new SingleSceneObjectReference();

        [DisplayName("Name script component\n" +
            "namespace required")]
        [DataMember]
        public string scriptName;

        [DisplayName("Name of the function")]
        [DataMember]
        public string methodName;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Execute function";
    }

    public override IStageProcess GetActivatingProcess()
    {
        return new ExecuteFuntionectsProcess(Data);
    }

    // --------------------------------------------------------------
    private class ExecuteFuntionectsProcess : StageProcess<EntityData>
    {
        public ExecuteFuntionectsProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            var go = Data.sceneObj?.Value.GameObject;

            if (go == null)
            {
                Debug.LogError("GameObject non assegnato!");
                return;
            }

            Type type = Type.GetType(Data.scriptName);
            Component comp = go.GetComponent(type);

            if (comp == null)
            {
                Debug.LogError($"{go.name} non ha il componente indicato!");
                return;
            }

            MethodInfo method = comp.GetType().GetMethod(Data.methodName);

            if (method != null)
            {
                method.Invoke(comp, null); // null = nessun parametro
            }
            else
            {
                Debug.LogWarning($"Metodo {Data.methodName} non trovato.");
            }
        }

        public override IEnumerator Update()
        {
            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }


    }


}

