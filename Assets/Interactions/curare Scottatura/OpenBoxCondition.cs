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
public class OpenBoxCondition : Condition<OpenBoxCondition.OpenBoxConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new OpenBoxConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new OpenBoxConditionAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    [DisplayName("Activate Alarm Button Pressed")]
    public class OpenBoxConditionData : IConditionData
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
    public class OpenBoxConditionAutocompleter : Autocompleter<OpenBoxConditionData>
    {
        public OpenBoxConditionAutocompleter(OpenBoxConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
public class OpenBoxConditionActiveProcess : StageProcess<OpenBoxConditionData>
{
    private Transform boxCover;  // riferimento al coperchio della scatola
    private float completionAngle = 10f; // angolo in gradi necessario per completare

    public OpenBoxConditionActiveProcess(OpenBoxConditionData data) : base(data) { }


    public override void Start()
    {
        var targetObject = Data.Target.Value?.GameObject;
        if (targetObject == null)
        {
            Debug.LogError("OpenBoxCondition: Target non assegnato!");
            return;
        }

        // Ottieni il transform del coperchio
        boxCover = targetObject.transform;
    }

    public override IEnumerator Update()
    {
        while (!Data.IsCompleted)
        {
            // Legge l'angolo locale sull'asse X
            float xRotation = boxCover.localEulerAngles.x;

                // Corregge l'angolo se supera 180 gradi (Unity usa 0-360)
                if (xRotation > 180f) xRotation -= 360f;
            
        

            if (xRotation <= completionAngle)
            {
                Data.IsCompleted = true;
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


    public OpenBoxCondition()
    {
        Data.Name = "OpenBox";
    }
}