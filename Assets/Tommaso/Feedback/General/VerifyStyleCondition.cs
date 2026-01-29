using System;
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;

/// <summary>
/// Condizione VR Builder che verifica se un determinato valore di stile di apprendimento
/// (tra le 4 dimensioni del modello FSLSM) è presente nel profilo corrente.
/// </summary>
[DataContract(IsReference = true)]
public class LearningStyleCondition : Condition<LearningStyleCondition.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IConditionData
    {
        [DisplayName("Valore atteso (verificato su tutte le dimensioni)")]
        [DataMember]
        public LearningValue expectedValue = LearningValue.Attivo;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        public bool IsCompleted { get; set; }

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Verifica valore nello stile di apprendimento";
    }

    /// <summary>
    /// Valori possibili per la verifica.  
    /// Corrispondono a quelli definiti in LearningEnums.
    /// </summary>
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

    public override IStageProcess GetActiveProcess() => new LearningStyleCheckProcess(Data);

    // =====================================================
    // PROCESSO DI VERIFICA
    // =====================================================
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

            // ✅ Controllo usando LearningEnums
            switch (Data.expectedValue)
            {
                case LearningValue.Attivo:
                    result = profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Attivo;
                    break;

                case LearningValue.Riflessivo:
                    result = profile.attivoRiflessivo == LearningEnums.AttivoRiflessivo.Riflessivo;
                    break;

                case LearningValue.Sensitivo:
                    result = profile.sensitivoIntuitivo == LearningEnums.SensitivoIntuitivo.Sensitivo;
                    break;

                case LearningValue.Intuitivo:
                    result = profile.sensitivoIntuitivo == LearningEnums.SensitivoIntuitivo.Intuitivo;
                    break;

                case LearningValue.Visivo:
                    result = profile.visivoVerbale == LearningEnums.VisivoVerbale.Visivo;
                    break;

                case LearningValue.Verbale:
                    result = profile.visivoVerbale == LearningEnums.VisivoVerbale.Verbale;
                    break;

                case LearningValue.Sequenziale:
                    result = profile.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Sequenziale;
                    break;

                case LearningValue.Globale:
                    result = profile.sequenzialeGlobale == LearningEnums.SequenzialeGlobale.Globale;
                    break;
            }

            if (result)
            {
                Debug.Log($"✅ [LearningStyleCondition] Valore '{Data.expectedValue}' trovato nel profilo utente.");
                Data.IsCompleted = true;
            }
            else
            {
                Debug.Log($"❌ [LearningStyleCondition] Nessuna corrispondenza per '{Data.expectedValue}'.");
            }

            yield return null;
        }

        public override void End() { }
        public override void FastForward() { }
    }
}
