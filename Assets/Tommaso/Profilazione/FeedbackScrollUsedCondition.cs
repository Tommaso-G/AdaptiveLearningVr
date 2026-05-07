using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;

[DataContract(IsReference = true)]
public class FeedbackScrollUsedCondition : Condition<FeedbackScrollUsedCondition.FeedbackScrollUsedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new FeedbackScrollUsedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new FeedbackScrollUsedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Feedback Scroll Used")]
    public class FeedbackScrollUsedConditionData : IConditionData
    {
        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Feedback Scroll Used";
        public bool IsCompleted { get; set; }

        [DataMember]
        public float Threshold { get; set; } = 0.05f;
    }

    public class FeedbackScrollUsedConditionAutocompleter : Autocompleter<FeedbackScrollUsedConditionData>
    {
        public FeedbackScrollUsedConditionAutocompleter(FeedbackScrollUsedConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    public class FeedbackScrollUsedConditionActiveProcess : StageProcess<FeedbackScrollUsedConditionData>
    {
        private Scrollbar scrollbar;

        private float initialValue;
        private bool triggered = false;
        private bool isInteracting = false;

        public FeedbackScrollUsedConditionActiveProcess(FeedbackScrollUsedConditionData data) : base(data) { }

        public override void Start()
        {
            scrollbar = GameObject.FindFirstObjectByType<Scrollbar>();

            if (scrollbar == null)
            {
                Debug.LogWarning("[FeedbackScrollUsedCondition] Nessuna Scrollbar trovata.");
                return;
            }

            Debug.Log("[FeedbackScrollUsedCondition] Scrollbar trovata: " + scrollbar.name);

            SetupPointerEvents(scrollbar);

            scrollbar.StartCoroutine(DelayedInit());
        }

        private void SetupPointerEvents(Scrollbar sb)
        {
            EventTrigger trigger = sb.gameObject.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = sb.gameObject.AddComponent<EventTrigger>();

            var entryDown = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            entryDown.callback.AddListener((data) => { isInteracting = true; });

            var entryUp = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            entryUp.callback.AddListener((data) => { isInteracting = false; });

            trigger.triggers.Add(entryDown);
            trigger.triggers.Add(entryUp);
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(2f);

            initialValue = scrollbar.value;

            Debug.Log($"Listener attivo. Valore iniziale: {initialValue}");

            scrollbar.onValueChanged.AddListener(OnScrollChanged);
        }

        private void OnScrollChanged(float value)
        {
            if (triggered)
                return;

            // 🔥 SOLO mentre l’utente sta interagendo
            if (!isInteracting)
                return;

            float delta = Mathf.Abs(value - initialValue);

            if (delta >= Data.Threshold)
            {
                Debug.Log($"Scroll rilevato durante interazione (delta: {delta})");

                triggered = true;

                scrollbar.StartCoroutine(DelayedCompletion());
            }
        }

        private IEnumerator DelayedCompletion()
        {
            yield return new WaitForSeconds(2f);

            Debug.Log("Scroll confermato → condition completata.");

            Data.IsCompleted = true;
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
                yield return null;
        }

        public override void End()
        {
            if (scrollbar != null)
                scrollbar.onValueChanged.RemoveListener(OnScrollChanged);
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }
}