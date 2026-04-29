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
public class ExecuteFuntionBehavior : Behavior<ExecuteFuntionBehavior.EntityData>
{

    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("GameObject with function to execute")]
        [DataMember]
        public SingleSceneObjectReference sceneObj { get; set; } = new SingleSceneObjectReference();

        [DisplayName("Name of the script component\n(namespace required)")]
        [DataMember]
        public string scriptName;

        [DisplayName("Name of the function")]
        [DataMember]
        public string methodName;

        [DisplayName("String parameter (optional)\nLeave empty for no parameter")]
        [DataMember]
        public string methodParameter;

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
                Debug.LogError("[ExecuteFunctionBehavior] GameObject non assegnato.");
                return;
            }

            Type type = Type.GetType(Data.scriptName);

            if (type == null)
            {
                Debug.LogError($"[ExecuteFunctionBehavior] Tipo '{Data.scriptName}' non trovato. Verifica il nome e il namespace.");
                return;
            }

            Component comp = go.GetComponent(type);

            if (comp == null)
            {
                Debug.LogError($"[ExecuteFunctionBehavior] '{go.name}' non ha il componente '{Data.scriptName}'.");
                return;
            }

            bool hasParameter = !string.IsNullOrEmpty(Data.methodParameter);

            // Cerca il metodo con o senza parametro stringa in base a methodParameter
            MethodInfo method = hasParameter
                ? comp.GetType().GetMethod(Data.methodName, new Type[] { typeof(string) })
                : comp.GetType().GetMethod(Data.methodName, new Type[] { });

            // Fallback: cerca per nome senza controllo firma
            if (method == null)
                method = comp.GetType().GetMethod(Data.methodName);

            if (method == null)
            {
                Debug.LogWarning($"[ExecuteFunctionBehavior] Metodo '{Data.methodName}' non trovato su '{Data.scriptName}'.");
                return;
            }

            object[] parameters = hasParameter
                ? new object[] { Data.methodParameter }
                : null;

            method.Invoke(comp, parameters);
        }

        public override IEnumerator Update()
        {
            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }
    }
}