using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class FeedbackManagerBehaviour : Behavior<FeedbackManagerBehaviour.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("Posizione (Scene Object)")]
        [DataMember]
        public SingleSceneObjectReference feedbackPosition { get; set; } = new SingleSceneObjectReference();

        [DisplayName("Capitolo corrente (index)")]
        [DataMember]
        public int ChapterIndex { get; set; } = 0;

        [DisplayName("Step corrente (index)")]
        [DataMember]
        public int StepIndex { get; set; } = 0;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Adaptive Feedback Manager";
    }

    public override IStageProcess GetActivatingProcess() => new FeedbackManagerProcess(Data);

    private class FeedbackManagerProcess : StageProcess<EntityData>
    {
        public FeedbackManagerProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            ApplyAdaptiveFeedback();
        }

        public override IEnumerator Update() { yield return null; }
        public override void End() { }
        public override void FastForward() { }

        private void ApplyAdaptiveFeedback()
        {
            // Trova il profilo utente
            LearningProfile profile = Object.FindFirstObjectByType<LearningProfile>();
            if (profile == null)
            {
                Debug.LogWarning("[FeedbackManager] Nessun LearningProfile trovato in scena.");
                return;
            }

            // Trova automaticamente il FeedbackSetHolder che punta al repository
            FeedbackSetHolder holder = Object.FindFirstObjectByType<FeedbackSetHolder>();
            if (holder == null || holder.Repository == null)
            {
                Debug.LogWarning("[FeedbackManager] Nessun FeedbackSetHolder valido trovato in scena.");
                return;
            }

            FeedbackRepository repository = holder.Repository;

            // Ottieni il prefab corretto dal repository
            GameObject prefab = repository.GetFeedbackPrefab(profile, Data.ChapterIndex, Data.StepIndex);
            if (prefab == null)
            {
                Debug.LogWarning($"[FeedbackManager] Nessun prefab trovato per Capitolo {Data.ChapterIndex}, Step {Data.StepIndex}.");
                return;
            }

            // Mostra il feedback e salva l’istanza nell’holder
            ShowFeedback(prefab, holder);
        }

        private void ShowFeedback(GameObject prefab, FeedbackSetHolder holder)
        {
            if (Data.feedbackPosition == null || Data.feedbackPosition.Value == null)
            {
                Debug.LogWarning("[FeedbackManager] Nessuna posizione assegnata per il feedback.");
                return;
            }

            Transform pos = Data.feedbackPosition.Value.GameObject.transform;
            GameObject feedbackUI = Object.Instantiate(prefab, pos.position, pos.rotation);
            

            // Salva l’istanza nell’holder così DestroyFeedbackBehaviour può accedervi
            holder.activeFeedbackInstance = feedbackUI;

            Debug.Log($"[FeedbackManager] Mostrato feedback prefab '{prefab.name}' al Capitolo {Data.ChapterIndex}, Step {Data.StepIndex}.");
        }
    }
}
