using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Scripting;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class OpenUiBehavior : Behavior<OpenUiBehavior.EntityData>
{
    [DisplayName("Open Ui Panel")]
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Open UI Panel";

        [DataMember]
        [DisplayName("UI Panels")]
        
        public MultipleSceneObjectReference UiPanels { get; set; } = new MultipleSceneObjectReference();

        public Metadata Metadata { get; set; }
    }

    public override IStageProcess GetActivatingProcess()
    {
        return new OpenUiProcess(Data);
    }

    // ----------------- PROCESS -----------------

    private class OpenUiProcess : StageProcess<EntityData>
    {
        private readonly MultipleSceneObjectReference panels;

        public OpenUiProcess(EntityData data) : base(data)
        {
            panels = data.UiPanels;
        }

        public override void Start()
        {
            ActivatePanels();
        }

        public override IEnumerator Update()
        {
            // Nessuna operazione continua: comportamento immediato
            yield break;
        }

        public override void End() { }

        public override void FastForward()
        {
            ActivatePanels();
        }

        private void ActivatePanels()
        {
            if (panels?.Values == null)
                return;

            List<ISceneObject> objects = panels.Values.ToList();
            foreach (var obj in objects)
            {
                if (obj?.GameObject != null)
                    obj.GameObject.SetActive(true);
            }
        }
    }
}

