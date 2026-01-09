using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
[DisplayName("Object Active State")]
public class IsActiveCondition : Condition<IsActiveCondition.ObjectActiveStateData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new ObjectActiveStateActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new ObjectActiveStateAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    public class ObjectActiveStateData : IConditionData
    {
        [DataMember]
        [DisplayName("Oggetto target")]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Attivo")]
        public bool DeveEssereAttivo { get; set; } = true;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Verifica Stato Oggetto";

        [IgnoreDataMember]
        public bool IsCompleted { get; set; }
    }

    // ============================
    // AUTO-COMPLETER
    // ============================
    public class ObjectActiveStateAutocompleter : Autocompleter<ObjectActiveStateData>
    {
        public ObjectActiveStateAutocompleter(ObjectActiveStateData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
    public class ObjectActiveStateActiveProcess : StageProcess<ObjectActiveStateData>
    {
        private GameObject targetObject;

        public ObjectActiveStateActiveProcess(ObjectActiveStateData data) : base(data) { }

        public override void Start()
        {
            targetObject = Data.Target.Value?.GameObject;
            if (targetObject == null)
            {
                Debug.LogError("[ObjectActiveStateCondition] Target non assegnato o nullo!");
                return;
            }
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
            {
                if (targetObject == null)
                {
                    Debug.LogWarning("[ObjectActiveStateCondition] Target distrutto o non più presente in scena.");
                    yield break;
                }

                bool statoAttuale = targetObject.activeSelf;

                if (Data.DeveEssereAttivo && statoAttuale)
                    Data.IsCompleted = true;
                else if (!Data.DeveEssereAttivo && !statoAttuale)
                    Data.IsCompleted = true;

                yield return null;
            }
        }

        public override void End() { }
        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }
}
