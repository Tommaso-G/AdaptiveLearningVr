using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.LowLevelPhysics;
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
        [DisplayName("Feedback Holder (Scene Object)")]
        [DataMember]
        public SingleSceneObjectReference feedbackHolder { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();


        [DisplayName("Posizione")]
        [DataMember]
        public SingleSceneObjectReference feedbackPosition { get; set; } = new SingleSceneObjectReference();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Adaptive Feedback Manager";

        [DisplayName("numero dello Step")]
        [DataMember]
        public int StepNumber { get; set; }
        
        
    }

    public override IStageProcess GetActivatingProcess() => new FeedbackManagerProcess(Data);


    // ---------------- PROCESS ----------------
    private class FeedbackManagerProcess : StageProcess<EntityData>
{
    private GameObject feedbackUI;

    public FeedbackManagerProcess(EntityData data) : base(data) { }

    public override void Start()
    {
        ApplyAdaptiveFeedback();
    }

    public override IEnumerator Update()
    {

        yield return null;
    }

    public override void End()
    {

    }

    public override void FastForward()
    {
        // Nel fast-forward distruggi subito il prefab se esiste
        if (feedbackUI != null)
        {
            Object.Destroy(feedbackUI);
            feedbackUI = null;
        }
    }

    private void ApplyAdaptiveFeedback()
    {
        // --- 1️⃣ Trova il profilo utente ---
        LearningProfile profile = Object.FindFirstObjectByType<LearningProfile>();
        if (profile == null)
        {
            Debug.LogWarning("[FeedbackManager] Nessun LearningProfile trovato in scena.");
            return;
        }

        // --- 2️⃣ Recupera l'holder in scena ---
        if (Data.feedbackHolder.Value == null)
        {
            Debug.LogWarning("[FeedbackManager] Nessun FeedbackSetHolder assegnato allo Step.");
            return;
        }

        FeedbackSetHolder holder = Data.feedbackHolder.Value.GameObject.GetComponent<FeedbackSetHolder>();
        if (holder == null || holder.set == null)
        {
            Debug.LogWarning("[FeedbackManager] L'oggetto assegnato non contiene un FeedbackSet valido.");
            return;
        }

        FeedbackSet set = holder.set;

        // --- 3️⃣ Seleziona il feedback corretto ---
        FeedbackData feedback = set.GetMatchingFeedback(profile, Data.StepNumber);
        if (feedback == null)
        {
            return;
        }

        // --- 4️⃣ Mostra il feedback UI ---
        ShowFeedback(feedback);
        holder.activeFeedbackInstance = feedbackUI;
    }

    private void ShowFeedback(FeedbackData data)
    {
        if (data.prefabUI != null)
        {
            if (Data.feedbackPosition == null)
            {
                Debug.LogWarning("[FeedbackManager] Nessun Transform assegnato come posizione del feedback.");
                return;
            }


            Transform pos = Data.feedbackPosition.Value.GameObject.transform;
            feedbackUI = Object.Instantiate(data.prefabUI, pos.position, pos.rotation);
            

           
            

            Debug.Log($"[FeedbackManager] Mostrato feedback davanti alla camera: {data.name}");
        }
        else
        {
            Debug.LogWarning($"[FeedbackManager] Nessun prefabUI assegnato in {data.name}.");
        }
    }

}

}
