using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
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
        public float Threshold { get; set; } = 0.05f; // minimo movimento richiesto
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

        scrollbar.StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        // Aspetta 2 secondi per evitare trigger da inizializzazione
        yield return new WaitForSeconds(2f);

        initialValue = scrollbar.value;

        Debug.Log($"Listener attivato dopo delay. Valore iniziale: {initialValue}");

        scrollbar.onValueChanged.AddListener(OnScrollChanged);
    }

        private void OnScrollChanged(float value)
        {
            if (triggered) return;

            float delta = Mathf.Abs(value - initialValue);

            if (delta >= Data.Threshold)
            {
                Debug.Log($"Scroll rilevato (delta: {delta}) → attendo 2 secondi...");
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