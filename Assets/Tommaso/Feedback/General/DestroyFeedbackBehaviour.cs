using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class DestroyFeedbackBehaviour : Behavior<DestroyFeedbackBehaviour.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Destroy Feedback";
    }

    public override IStageProcess GetActivatingProcess() => new DestroyFeedbackProcess(Data);

    // -------- PROCESS --------
    private class DestroyFeedbackProcess : StageProcess<EntityData>
    {
        public DestroyFeedbackProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            // Trova automaticamente il FeedbackSetHolder in scena
            FeedbackSetHolder holder = Object.FindFirstObjectByType<FeedbackSetHolder>();
            if (holder == null)
            {
                Debug.LogWarning("[DestroyFeedback] Nessun FeedbackSetHolder valido trovato in scena.");
                return;
            }

            // Se l’holder ha un riferimento al feedback istanziato, distruggilo
            if (holder.activeFeedbackInstance != null)
            {
                Object.Destroy(holder.activeFeedbackInstance);
                holder.activeFeedbackInstance = null;
                Debug.Log("[DestroyFeedback] Feedback distrutto con successo.");
            }
            else
            {
                Debug.Log("[DestroyFeedback] Nessun feedback attivo da distruggere.");
            }
        }

        public override IEnumerator Update() { yield break; }
        public override void End() { }
        public override void FastForward() { }
    }
}
