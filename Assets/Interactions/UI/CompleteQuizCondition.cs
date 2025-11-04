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
public class CompleteQuizCondition : Condition<CompleteQuizCondition.CompleteQuizConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new CompleteQuizConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new CompleteQuizConditionAutocompleter(Data);
    }

    // ============================
    // DATI DELLA CONDIZIONE
    // ============================
    [DataContract(IsReference = true)]
    [DisplayName("Complete Quiz")]
    public class CompleteQuizConditionData : IConditionData
    {
        [DataMember]
        public SingleSceneObjectReference Target { get; set; } = new SingleSceneObjectReference();

        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Complete Quiz";
        public bool IsCompleted { get; set; }
    }

    // ============================
    // AUTO-COMPLETER
    // ============================
    public class CompleteQuizConditionAutocompleter : Autocompleter<CompleteQuizConditionData>
    {
        public CompleteQuizConditionAutocompleter(CompleteQuizConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    // ============================
    // PROCESSO ATTIVO
    // ============================
    public class CompleteQuizConditionActiveProcess : StageProcess<CompleteQuizConditionData>
    {
        private QuizManager quizManager;  // riferimento al bottone

        public CompleteQuizConditionActiveProcess(CompleteQuizConditionData data) : base(data) { }

        public override void Start()
        {
            // Ottiene l’oggetto target di VR Builder
            var targetObject = Data.Target.Value?.GameObject;
            if (targetObject == null)
            {
                Debug.LogError("CompleteQuizCondition: Target non assegnato!");
                return;
            }

            // Cerca il componente XRPushButtonVrBuilder
            quizManager = targetObject.GetComponent<QuizManager>();
            if (quizManager == null)
            {
                Debug.LogError("CompleteQuizCondition: L’oggetto target non ha un componente QuizManager!");
                return;
            }

            // Aggiunge il listener all’evento onPress
            quizManager.OnEnd.AddListener(OnQuizCompleted);
        }

        private void OnQuizCompleted()
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
            if (quizManager != null)
            {
                // Rimuove il listener quando la condizione termina
                quizManager.OnEnd.RemoveListener(OnQuizCompleted);
            }
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }

    public CompleteQuizCondition()
    {
        Data.Name = "CompleteQuiz";
    }
}
