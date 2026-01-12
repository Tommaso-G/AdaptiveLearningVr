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
        private Collider corridorCollider;
        private Camera mainCamera;

        public override void Start()
        {
            doorScript = Data.Target.Value.GameObject.GetComponent<ClosableDoor>();
            mainCamera = Camera.main;

            if (doorScript == null)
            {
                Debug.LogWarning($"[DoorCondition] L'oggetto '{Data.Target.Value.GameObject.name}' non ha un componente ClosableDoor!");
            }

            // Trova il collider del corridoio
            GameObject corridorObject = GameObject.Find("Corridoio");
            if (corridorObject != null)
            {
                corridorCollider = corridorObject.GetComponent<Collider>();
                if (corridorCollider == null)
                    Debug.LogWarning("[DoorCondition] L'oggetto 'corridoio' non ha un Collider!");
            }
            else
            {
                Debug.LogWarning("[DoorCondition] Nessun oggetto chiamato 'corridoio' trovato nella scena!");
            }
        }

        public override IEnumerator Update()
        {
            // Continua finché la porta non è chiusa o la camera non è nel corridoio
            while (!IsConditionSatisfied())
            {
                yield return null;
            }

            // Quando entrambe le condizioni sono vere
            Data.IsCompleted = true;
        }

        private bool IsConditionSatisfied()
        {
            if (doorScript == null || mainCamera == null || corridorCollider == null)
                return false;

            bool doorClosed = doorScript.IsClosed;
            bool cameraInsideCorridor = corridorCollider.bounds.Contains(mainCamera.transform.position);

            return doorClosed && cameraInsideCorridor;
        }

        public override void End() { }

        public override void FastForward() { }

        public DoorClosedActiveProcess(DoorClosedData data) : base(data) { }
    }


   public DoorClosed()
   {
       Data.Name = "Door Closed";
   }
}
