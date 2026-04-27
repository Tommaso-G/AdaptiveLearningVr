using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class FollowPlayerBehavior : Behavior<FollowPlayerBehavior.EntityData>
{
    // 🔔 Evento statico: (follower, player)
    public static event System.Action<GameObject, GameObject> OnFollowPlayerTriggered;

    [DisplayName("Follow Player")]
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Follow Player";

        [DataMember]
        [DisplayName("Follower Object")]
        public SingleSceneObjectReference Follower { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Player")]
        public SingleSceneObjectReference Player { get; set; } = new SingleSceneObjectReference();

        public Metadata Metadata { get; set; }
    }

    public override IStageProcess GetActivatingProcess() => new FollowPlayerProcess(Data);

    private class FollowPlayerProcess : StageProcess<EntityData>
    {
   

        public FollowPlayerProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            var follower = Data.Follower.Value?.GameObject;
            var player = Data.Player.Value?.GameObject;

            if (follower == null || player == null)
            {
                Debug.LogWarning("FollowPlayerBehavior: follower o player non assegnato!");

                return;
            }
            Debug.Log($"[FollowPlayerBehavior] Invoco evento — follower: {follower?.name}, player: {player?.name}");

            // Emetti l'evento
            OnFollowPlayerTriggered?.Invoke(follower, player);


        }

        public override IEnumerator Update()
        {
            // Terminato immediatamente
            yield break;
        }

        public override void End() { }

        public override void FastForward() { }

    }
}
