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
public class DoorClosed : Condition<DoorClosed.DoorClosedData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new DoorClosedActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new DoorClosedAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Is Door Closed?")]
    public class DoorClosedData : IConditionData
    {
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

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
        private ExitDoor exitDoorScript;
        private Collider corridorCollider;
        private Camera mainCamera;

        public override void Start()
        {
            mainCamera = Camera.main;

            if (Data.Target.Value != null && Data.Target.Value.GameObject != null)
            {
                GameObject target = Data.Target.Value.GameObject;

                // Prova prima ClosableDoor
                doorScript = target.GetComponent<ClosableDoor>();
                if (doorScript != null)
                {
                    Debug.Log($"[DoorCondition] ClosableDoor trovato su: {target.name}");
                }
                else
                {
                    // Prova ExitDoor
                    exitDoorScript = target.GetComponent<ExitDoor>();
                    if (exitDoorScript != null)
                        Debug.Log($"[DoorCondition] ExitDoor trovato su: {target.name}");
                    else
                        Debug.LogWarning($"[DoorCondition] Nessun componente ClosableDoor o ExitDoor trovato su '{target.name}'!");
                }
            }
            else
            {
                Debug.LogWarning("[DoorCondition] Nessun Target assegnato!");
            }

            // Trova il collider del corridoio
            GameObject corridorObject = GameObject.Find("CorridoioCollider");
            if (corridorObject != null)
            {
                corridorCollider = corridorObject.GetComponent<Collider>();
                if (corridorCollider == null)
                    Debug.LogWarning("[DoorCondition] L'oggetto 'CorridoioCollider' non ha un Collider!");
            }
            else
            {
                Debug.LogWarning("[DoorCondition] Nessun oggetto chiamato 'CorridoioCollider' trovato nella scena!");
            }

            Debug.Log("[DoorCondition] Start() completato");
        }

        public override IEnumerator Update()
        {
            Debug.Log("[DoorCondition] Update() avviato");

            while (!IsConditionSatisfied())
            {
                yield return null;
            }

            Debug.Log("[DoorCondition] Condizione soddisfatta, completamento step");
            Data.IsCompleted = true;
        }

        private bool IsConditionSatisfied()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("[DoorCondition] mainCamera è null!");
                return false;
            }

            if (corridorCollider == null)
            {
                Debug.LogWarning("[DoorCondition] corridorCollider è null!");
                return false;
            }

            if (doorScript == null && exitDoorScript == null)
            {
                Debug.LogWarning("[DoorCondition] Nessuna porta trovata!");
                return false;
            }

            bool doorClosed = doorScript != null ? doorScript.IsClosed : exitDoorScript.isDoorClosed;
            bool cameraInsideCorridor = corridorCollider.bounds.Contains(mainCamera.transform.position);

            //Debug.Log($"[DoorCondition] doorClosed={doorClosed} | cameraInsideCorridor={cameraInsideCorridor}");

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