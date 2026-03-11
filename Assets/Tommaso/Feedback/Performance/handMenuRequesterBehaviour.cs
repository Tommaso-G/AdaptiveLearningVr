using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;

[DataContract(IsReference = true)]
public class OpenHandMenuBehavior : Behavior<OpenHandMenuBehavior.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [DataMember]
        public string MenuId { get; set; } = "";

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Open Hand Menu";
    }

    public override IStageProcess GetActivatingProcess() => new OpenHandMenuProcess(Data);

    private class OpenHandMenuProcess : StageProcess<EntityData>
    {
        public OpenHandMenuProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            HandMenu handMenu = Object.FindFirstObjectByType<HandMenu>();
            if (handMenu == null)
            {
                Debug.LogWarning("[OpenHandMenuBehavior] Nessun HandMenu trovato in scena.");
                return;
            }

            if (string.IsNullOrEmpty(Data.MenuId))
            {
                Debug.LogWarning("[OpenHandMenuBehavior] MenuId non impostato.");
                return;
            }

            handMenu.RequestOpen(Data.MenuId);
            Debug.Log($"[OpenHandMenuBehavior] RequestOpen chiamato con id: '{Data.MenuId}'");
        }

        public override IEnumerator Update() { yield break; }
        public override void End() { }
        public override void FastForward() { }
    }
}