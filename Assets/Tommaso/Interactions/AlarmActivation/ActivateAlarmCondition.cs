using System;
using System.Collections;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;

[DataContract(IsReference = true)]
public class ActivateAlarmCondition : Condition<ActivateAlarmCondition.ActivateAlarmConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new ActivateAlarmConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new ActivateAlarmConditionAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    [DisplayName("Activate Alarm Button Pressed")]
    public class ActivateAlarmConditionData : IConditionData
    {
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Activate Alarm";
        public bool IsCompleted { get; set; }
    }

    // ============================
    // AUTO-COMPLETER
    // ============================
    public class ActivateAlarmConditionAutocompleter : Autocompleter<ActivateAlarmConditionData>
    {
        public ActivateAlarmConditionAutocompleter(ActivateAlarmConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
    public class ActivateAlarmConditionActiveProcess : StageProcess<ActivateAlarmConditionData>
    {
        private XRPushButtonVrBuilder button;  // riferimento al bottone

        public ActivateAlarmConditionActiveProcess(ActivateAlarmConditionData data) : base(data) { }

        public override void Start()
        {
            // Ottiene l’oggetto target di VR Builder
            var targetObject = Data.Target.Value?.GameObject;
            if (targetObject == null)
            {
                Debug.LogError("ActivateAlarmCondition: Target non assegnato!");
                return;
            }

            // Cerca il componente XRPushButtonVrBuilder
            button = targetObject.GetComponent<XRPushButtonVrBuilder>();
            if (button == null)
            {
                Debug.LogError("ActivateAlarmCondition: L’oggetto target non ha un componente XRPushButtonVrBuilder!");
                return;
            }

            // Aggiunge il listener all’evento onPress
            button.onPress.AddListener(OnButtonPressed);
        }

        private void OnButtonPressed()
        {
            Debug.Log("✅ Pulsante premuto: condizione completata.");
            Data.IsCompleted = true;
        }

        public override IEnumerator Update()
        {
            // Attende finché la condizione non è completata
            while (!Data.IsCompleted)
            {
                yield return null;
            }
        }

        public override void End()
        {
            if (button != null)
            {
                // Rimuove il listener quando la condizione termina
                button.onPress.RemoveListener(OnButtonPressed);
            }
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }

    public ActivateAlarmCondition()
    {
        Data.Name = "Activate Alarm";
    }
}
