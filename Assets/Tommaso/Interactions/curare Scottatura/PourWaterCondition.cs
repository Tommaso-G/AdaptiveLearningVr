using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class PourWaterCondition : Condition<PourWaterCondition.PourWaterConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new PourWaterConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new PourWaterConditionAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    [DisplayName("Pour Water Condition")]
    public class PourWaterConditionData : IConditionData
    {
        [DataMember]
        [DisplayName("Oggetto da colpire")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Particle System da contare")]
        public SingleSceneObjectReference ParticleSystemReference { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Numero di particelle richieste")]
        public int RequiredHits { get; set; } = 100;

        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Pour Water";
        public bool IsCompleted { get; set; }
    }

    // ============================
    // AUTO-COMPLETER
    // ============================
    public class PourWaterConditionAutocompleter : Autocompleter<PourWaterConditionData>
    {
        public PourWaterConditionAutocompleter(PourWaterConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
    public class PourWaterConditionActiveProcess : StageProcess<PourWaterConditionData>
    {
        private ParticleSystem particleSystem;
        private GameObject targetObject;
        private int hitCount = 0;
        private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

        public PourWaterConditionActiveProcess(PourWaterConditionData data) : base(data) { }

        public override void Start()
        {
            targetObject = Data.Target.Value?.GameObject;
            if (targetObject == null)
            {
                Debug.LogError("PourWaterCondition: Target non assegnato!");
                return;
            }

            var psGO = Data.ParticleSystemReference.Value?.GameObject;
            if (psGO == null)
            {
                Debug.LogError("PourWaterCondition: ParticleSystem non assegnato!");
                return;
            }

            particleSystem = psGO.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                Debug.LogError("PourWaterCondition: il GameObject non ha ParticleSystem!");
                return;
            }

            // Assicurati che il ParticleSystem abbia Collision attiva e Send Collision Messages = true
            var collision = particleSystem.collision;
            collision.enabled = true;
            collision.sendCollisionMessages = true;
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
            {
                if (particleSystem != null && targetObject != null)
                {
                    int numCollisions = particleSystem.GetCollisionEvents(targetObject, collisionEvents);
                    hitCount += numCollisions;

                    Debug.Log($"Frame: {Time.frameCount} | Particelle colpite questo frame: {numCollisions} | Totale: {hitCount}");


                    if (hitCount >= Data.RequiredHits)
                    {
                        Data.IsCompleted = true;
                    }
                }

                yield return null;
            }
        }

        public override void End() { }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }

    public PourWaterCondition()
    {
        Data.Name = "PourWater";
    }
}
