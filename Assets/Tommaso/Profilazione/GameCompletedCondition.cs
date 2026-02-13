using System;
using System.Collections;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
[DisplayName("Is Game Completed?")]
public class GameCompletedCondition : Condition<GameCompletedCondition.GameCompletedData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new GameCompletedActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new GameCompletedAutocompleter(Data);
    }

    // ================================================================
    // DATA
    // ================================================================
    [DataContract(IsReference = true)]
    public class GameCompletedData : IConditionData
    {
        [DataMember]
        [DisplayName("Target Game Object")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public string Name { get; set; }

        [DataMember]
        public Metadata Metadata { get; set; }

        
        public bool IsCompleted { get; set; }
    }

    // ================================================================
    // AUTOCOMPLETER
    // ================================================================
    public class GameCompletedAutocompleter : Autocompleter<GameCompletedData>
    {
        public GameCompletedAutocompleter(GameCompletedData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ================================================================
    // ACTIVE PROCESS
    // ================================================================
    public class GameCompletedActiveProcess : StageProcess<GameCompletedData>
    {
        private ICompletableStep gameScript;

        public GameCompletedActiveProcess(GameCompletedData data) : base(data) { }

        public override void Start()
        {
            // Recupera il componente che implementa ICompletableGame dal GameObject assegnato
            if (Data.Target?.Value == null)
            {
                Debug.LogWarning("[GameCompletedCondition] Nessun target assegnato!");
                return;
            }

            gameScript = Data.Target.Value.GameObject.GetComponent<ICompletableStep>();

            if (gameScript == null)
            {
                Debug.LogWarning($"[GameCompletedCondition] Il target '{Data.Target.Value.GameObject.name}' non implementa ICompletableGame!");
            }
        }

        public override IEnumerator Update()
        {
            if (gameScript == null)
                yield break;

            // Rimani in attesa finché il gioco non è completato
            while (!gameScript.IsCompleted)
            {
                yield return null;
            }

            // Quando IsCompleted diventa true
            Data.IsCompleted = true;
            Debug.Log($"[GameCompletedCondition] '{Data.Target.Value.GameObject.name}' completato!");
        }

        public override void End() { }

        public override void FastForward()
        {
            // Se il target è già completato, salta direttamente
            if (gameScript != null && gameScript.IsCompleted)
            {
                Data.IsCompleted = true;
            }
        }
    }

    // ================================================================
    // COSTRUTTORE PRINCIPALE
    // ================================================================
    public GameCompletedCondition()
    {
        Data.Name = "Step completed";
    }
}
