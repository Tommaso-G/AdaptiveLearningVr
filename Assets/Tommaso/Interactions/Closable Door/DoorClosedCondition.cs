using System;
using System.Collections;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using VRBuilder.Core.Conditions;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using UnityEngine.XR.Content.Interaction;

[DataContract(IsReference = true)]
public class DoorClosed: Condition<DoorClosed.DoorClosedData>
{
   public override IStageProcess GetActiveProcess()
   {
       // Always return a new instance.
       return new DoorClosedActiveProcess(Data);
   }

   protected override IAutocompleter GetAutocompleter()
   {
       // Always return a new instance.
       return new DoorClosedAutocompleter(Data);
   }
   
   [DataContract(IsReference = true)]
   [DisplayName("Is Door Closed?")]
   public class DoorClosedData : IConditionData
   {
       // A reference to the target object that we will check.
       [DataMember]
       public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

       // We will check how far the target from being upside down in degrees.
       // If the difference is lower than threshold, we must complete the condition.
       [DataMember]
       public float Threshold { get; set; }

       public Metadata Metadata { get; set; }
       public string Name { get; set; }
       public bool IsCompleted { get; set; }
   }
   
   public class DoorClosedAutocompleter : Autocompleter<DoorClosedData>
   {
       public DoorClosedAutocompleter(DoorClosedData data) : base(data)
       {
       }

       public override void Complete()
       {
           Data.IsCompleted = true;
       }
   }
   
   public class DoorClosedActiveProcess : StageProcess<DoorClosedData>
    {
       private ClosableDoor doorScript;
       public override void Start()
        {
            doorScript = Data.Target.Value.GameObject.GetComponent<ClosableDoor>();

            if (doorScript == null)
            {
                Debug.LogWarning($"[Doorcondition] L'oggetto '{Data.Target.Value.GameObject.name}' non ha un componente Door!");
            }
        }

       public override IEnumerator Update()
       {
           // Get the difference between vector pointing down,
           // And the vector that comes out of the "roof" of the target.
           // Then compare it with the threshold from data.
            while (doorScript != null && !doorScript.IsClosed)
            {
                yield return null;
            }

        // Una volta afferrato, segna la condizione come completata
            Data.IsCompleted = true;
       }

       public override void End()
       {
       }

       // Nothing to fast-forward.
       // We will explain it soon.
       public override void FastForward()
       {
       }

       // Declare the constructor. It calls the base method to bind the data object with the process.
       public DoorClosedActiveProcess(DoorClosedData data) : base(data)
       {
       }
   }

   public DoorClosed()
   {
       Data.Name = "Door Closed";
   }
}
