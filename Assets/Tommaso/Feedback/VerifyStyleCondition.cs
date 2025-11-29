using System;
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;

[DataContract(IsReference = true)]
public class LearningStyleCondition : Condition<LearningStyleCondition.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IConditionData
    {
        [DataMember]
        [DisplayName("Valore atteso (verificato su tutte le dimensioni)")]
        public LearningValue expectedValue = LearningValue.Attivo;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        public bool IsCompleted { get; set; }

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Verifica valore nello stile di apprendimento";
    }

    public enum LearningValue
    {
        Attivo,
        Riflessivo,
        Sensitivo,
        Intuitivo,
        Visivo,
        Verbale,
        Sequenziale,
        Globale
    }

    public override IStageProcess GetActiveProcess()
    {
        return new LearningStyleCheckProcess(Data);
    }

    private class LearningStyleCheckProcess : StageProcess<EntityData>
    {
        public LearningStyleCheckProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            Debug.Log("[LearningStyleCondition] Avvio verifica dello stile di apprendimento...");
        }

        public override IEnumerator Update()
        {
            LearningProfile profile = UnityEngine.Object.FindFirstObjectByType<LearningProfile>();
            if (profile == null)
            {
                Debug.LogWarning("[LearningStyleCondition] Nessun LearningProfile trovato in scena!");
                yield break;
            }

            bool result = false;

            switch (Data.expectedValue)
            {
                case LearningValue.Attivo:
                    result = profile.attivoRiflessivo == LearningProfile.AttivoRiflessivo.Attivo;
                    break;
                case LearningValue.Riflessivo:
                    result = profile.attivoRiflessivo == LearningProfile.AttivoRiflessivo.Riflessivo;
                    break;
                case LearningValue.Sensitivo:
                    result = profile.sensitivoIntuitivo == LearningProfile.SensitivoIntuitivo.Sensitivo;
                    break;
                case LearningValue.Intuitivo:
                    result = profile.sensitivoIntuitivo == LearningProfile.SensitivoIntuitivo.Intuitivo;
                    break;
                case LearningValue.Visivo:
                    result = profile.visivoVerbale == LearningProfile.VisivoVerbale.Visivo;
                    break;
                case LearningValue.Verbale:
                    result = profile.visivoVerbale == LearningProfile.VisivoVerbale.Verbale;
                    break;
                case LearningValue.Sequenziale:
                    result = profile.sequenzialeGlobale == LearningProfile.SequenzialeGlobale.Sequenziale;
                    break;
                case LearningValue.Globale:
                    result = profile.sequenzialeGlobale == LearningProfile.SequenzialeGlobale.Globale;
                    break;
            }

            if (result)
            {
                Debug.Log($"[LearningStyleCondition] Valore {Data.expectedValue} trovato nel profilo utente.");
                Data.IsCompleted = true;
            }
            else
            {
                Debug.Log($"[LearningStyleCondition] Nessuna corrispondenza per {Data.expectedValue}.");
            }

            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }
    }
}
