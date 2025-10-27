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
public class GrabbedCondition : Condition<GrabbedCondition.GrabbedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        // Quando la condizione è attiva, crea e ritorna un nuovo processo che la controllerà.
        return new GrabbedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        // Restituisce un autocompleter: serve se la condizione deve “auto-completarsi”
        // quando si salta uno step o si forza il completamento.
        return new GrabbedConditionAutocompleter(Data);
    }

    // ----------------------------
    // DATI DELLA CONDIZIONE
    // ----------------------------
    [DataContract(IsReference = true)]
    [DisplayName("Is Object Grabbed?")]
    public class GrabbedConditionData : IConditionData
    {
    // Oggetto  della scena che vogliamo monitorare.
    [DataMember]
    public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();


        // Dati richiesti dall’interfaccia IConditionData
        public Metadata Metadata { get; set; }
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
    }

    // ----------------------------
    // AUTO-COMPLETER
    // ----------------------------
    public class GrabbedConditionAutocompleter : Autocompleter<GrabbedConditionData>
    {
        public GrabbedConditionAutocompleter(GrabbedConditionData data) : base(data) { }

        public override void Complete()
        {

            //segniamo la condizione come completata.
            Data.IsCompleted = true;
        }
    }

    // ----------------------------
    // PROCESSO ATTIVO (LOGICA REALE)
    // ----------------------------
    public class GrabbedConditionActiveProcess : StageProcess<GrabbedConditionData>
    {
        private XRGrabNoY grabScript;

        public GrabbedConditionActiveProcess(GrabbedConditionData data) : base(data) { }

        public override void Start()
        {
        // Ottiene il componente XRGrabNoY dall’oggetto target
            grabScript = Data.Target.Value.GameObject.GetComponent<XRGrabNoY>();

            if (grabScript == null)
            {
                Debug.LogWarning($"[GrabbedCondition] L'oggetto '{Data.Target.Value.GameObject.name}' non ha un componente XRGrabNoY!");
            }
        }

        public override IEnumerator Update()
        {
            // Attende finché il valore IsGrabbed non diventa true
            while (grabScript != null && !grabScript.IsGrabbed)
            {
                yield return null;
            }

        // Una volta afferrato, segna la condizione come completata
            Data.IsCompleted = true;
        }

        public override void End()
        {
        // Nessuna pulizia necessaria in questo caso
        }

        public override void FastForward()
        {
        // Se si forza il completamento dello step
        Data.IsCompleted = true;
        }
    }   


    public GrabbedCondition()
    {
        Data.Name = "Object Grabbed";
    }
}
