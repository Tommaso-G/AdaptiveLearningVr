using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class FeedbackButtonClickedCondition : Condition<FeedbackButtonClickedCondition.FeedbackButtonClickedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new FeedbackButtonClickedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new FeedbackButtonClickedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Feedback Button Clicked")]
    public class FeedbackButtonClickedConditionData : IConditionData
    {
        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Feedback Button Clicked";
        public bool IsCompleted { get; set; }

        [DataMember]
        [DisplayName("Target Position Object")]
        public SingleSceneObjectReference TargetPosition { get; set; } = new SingleSceneObjectReference();

        [DataMember]
        [DisplayName("Raggio di ricerca feedback (m)")]
        public float SearchRadius { get; set; } = 1.5f;
    }

    public class FeedbackButtonClickedConditionAutocompleter : Autocompleter<FeedbackButtonClickedConditionData>
    {
        public FeedbackButtonClickedConditionAutocompleter(FeedbackButtonClickedConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    public class FeedbackButtonClickedConditionActiveProcess : StageProcess<FeedbackButtonClickedConditionData>
    {
        private Button[] listenedButtons;
        private bool buttonClicked = false;

        public FeedbackButtonClickedConditionActiveProcess(FeedbackButtonClickedConditionData data) : base(data) { }

        public override void Start()
        {
            // Se non c'è target, completa subito
            if (Data.TargetPosition?.Value == null)
            {
                Debug.LogWarning("[FeedbackButtonClickedCondition] TargetPosition non assegnato, condizione completata automaticamente.");
                Data.IsCompleted = true;
                return;
            }

            Vector3 targetPos = Data.TargetPosition.Value.GameObject.transform.position;

            // Cerca tutti i FeedbackPrefabController nella scena
            FeedbackPrefabController[] allControllers = GameObject.FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);

            FeedbackPrefabController found = null;
            float bestDist = float.MaxValue;

            foreach (var ctrl in allControllers)
            {
                float dist = Vector3.Distance(ctrl.transform.position, targetPos);
                if (dist <= Data.SearchRadius && dist < bestDist)
                {
                    bestDist = dist;
                    found = ctrl;
                }
            }

            if (found == null)
            {
                Debug.LogWarning($"[FeedbackButtonClickedCondition] Nessun FeedbackPrefabController trovato entro {Data.SearchRadius}m dalla posizione target. Condizione completata automaticamente.");
                Data.IsCompleted = true;
                return;
            }

            // Se il feedback non richiede il bottone, completa subito
            if (!found.needsButtonToBeCompleted)
            {
                Debug.Log($"[FeedbackButtonClickedCondition] '{found.name}' non richiede bottone. Condizione completata automaticamente.");
                Data.IsCompleted = true;
                return;
            }

            // Cerca il campo buttonsToClickCanvas
            if (found.buttonsToClickCanvas == null)
            {
                Debug.LogWarning($"[FeedbackButtonClickedCondition] '{found.name}' ha NeedsButtonToBeCompleted=true ma buttonsToClickCanvas è null. Condizione completata automaticamente.");
                Data.IsCompleted = true;
                return;
            }

            // Prendi tutti i Button figli del canvas
            listenedButtons = found.buttonsToClickCanvas.GetComponentsInChildren<Button>(true);

            if (listenedButtons == null || listenedButtons.Length == 0)
            {
                Debug.LogWarning($"[FeedbackButtonClickedCondition] Nessun Button trovato in buttonsToClickCanvas di '{found.name}'. Condizione completata automaticamente.");
                Data.IsCompleted = true;
                return;
            }

            // Registra listener su ogni bottone
            foreach (var btn in listenedButtons)
            {
                btn.onClick.AddListener(OnButtonClicked);
                Debug.Log($"[FeedbackButtonClickedCondition] Listener registrato su '{btn.name}'.");
            }
        }

        private void OnButtonClicked()
        {
            if (buttonClicked) return;

            buttonClicked = true;
            Debug.Log("[FeedbackButtonClickedCondition] Bottone cliccato → condizione completata.");
            Data.IsCompleted = true;
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
                yield return null;
        }

        public override void End()
        {
            if (listenedButtons == null) return;

            foreach (var btn in listenedButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }
}