using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;


[DataContract(IsReference = true)]
public class FeedbackPageChangedCondition : Condition<FeedbackPageChangedCondition.FeedbackPageChangedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new FeedbackPageChangedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new FeedbackPageChangedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Feedback Page Changed")]
    public class FeedbackPageChangedConditionData : IConditionData
    {
        public Metadata Metadata { get; set; }
        public string Name { get; set; } = "Feedback Page Changed";
        public bool IsCompleted { get; set; }

        [DataMember]
        public int TargetPageIndex { get; set; } = 0; 
    }

    public class FeedbackPageChangedConditionAutocompleter : Autocompleter<FeedbackPageChangedConditionData>
    {
        public FeedbackPageChangedConditionAutocompleter(FeedbackPageChangedConditionData data) : base(data) { }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

public class FeedbackPageChangedConditionActiveProcess : StageProcess<FeedbackPageChangedConditionData>
{
    private PageToggleLinkerIndexed _linker;
    private bool isWaiting = false;

    public FeedbackPageChangedConditionActiveProcess(FeedbackPageChangedConditionData data) : base(data) { }

    public override void Start()
    {

        _linker = GameObject.FindFirstObjectByType<PageToggleLinkerIndexed>();
        

        if (_linker == null)
        {
            Debug.LogWarning("[FeedbackPageChangedCondition] Nessun linker trovato.");
            return;
        }

        _linker.OnPageChanged += OnPageChanged;
    }

    private void OnPageChanged(int index)
    {
        if (index == Data.TargetPageIndex && !isWaiting)
        {
            Debug.Log($"Pagina {index} raggiunta → attendo 3 secondi...");
            isWaiting = true;

            // Avvia coroutine tramite MonoBehaviour
            _linker.StartCoroutine(DelayedCompletion());
        }
    }

    private IEnumerator DelayedCompletion()
    {
        yield return new WaitForSeconds(3f);

        Debug.Log("Attesa completata → condition completata.");
        Data.IsCompleted = true;
    }

    public override IEnumerator Update()
    {
        while (!Data.IsCompleted)
            yield return null;
    }

    public override void End()
    {
        if (_linker != null)
            _linker.OnPageChanged -= OnPageChanged;
    }

    public override void FastForward()
    {
        Data.IsCompleted = true;
    }
}
}