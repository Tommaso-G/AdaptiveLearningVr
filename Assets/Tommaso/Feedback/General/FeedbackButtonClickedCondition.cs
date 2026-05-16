using System.Collections;
using System.Collections.Generic;
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
        // Mappa ogni bottone al suo controller, per sapere quali controller sono stati confermati
        private Dictionary<Button, FeedbackPrefabController> buttonToController = new();
        private HashSet<FeedbackPrefabController> completedControllers = new();
        private HashSet<FeedbackPrefabController> pendingControllers = new();

        public FeedbackButtonClickedConditionActiveProcess(FeedbackButtonClickedConditionData data) : base(data) { }

    public override void Start()
    {
        if (Data.TargetPosition?.Value == null)
        {
            Debug.LogWarning("[FeedbackButtonClickedCondition] TargetPosition non assegnato, condizione completata automaticamente.");
            Data.IsCompleted = true;
            return;
        }

        Transform targetTransform = Data.TargetPosition.Value.GameObject.transform;

        // Costruisce la lista dei centri di ricerca:
        // se il target ha figli, usa la posizione di ciascun figlio,
        // altrimenti usa il target stesso
        List<Vector3> searchOrigins = new();

        if (targetTransform.childCount > 0)
        {
            foreach (Transform child in targetTransform)
                searchOrigins.Add(child.position);
        }
        else
        {
            searchOrigins.Add(targetTransform.position);
        }

        FeedbackPrefabController[] allControllers = GameObject.FindObjectsByType<FeedbackPrefabController>(FindObjectsSortMode.None);

        // Un controller viene incluso se è entro raggio da ALMENO uno dei centri di ricerca
        List<FeedbackPrefabController> foundControllers = new();
        foreach (var ctrl in allControllers)
        {
            foreach (var origin in searchOrigins)
            {
                if (Vector3.Distance(ctrl.transform.position, origin) <= Data.SearchRadius)
                {
                    foundControllers.Add(ctrl);
                    break; // evita duplicati se il controller è vicino a più origini
                }
            }
        }

        if (foundControllers.Count == 0)
        {
            Debug.LogWarning($"[FeedbackButtonClickedCondition] Nessun FeedbackPrefabController trovato entro {Data.SearchRadius}m. Condizione completata automaticamente.");
            Data.IsCompleted = true;
            return;
        }

        foreach (var ctrl in foundControllers)
        {
            if (!ctrl.needsButtonToBeCompleted)
            {
                Debug.Log($"[FeedbackButtonClickedCondition] '{ctrl.name}' non richiede bottone, ignorato.");
                continue;
            }

            if (ctrl.buttonsToClickCanvas == null)
            {
                Debug.LogWarning($"[FeedbackButtonClickedCondition] '{ctrl.name}' richiede bottone ma buttonsToClickCanvas è null, ignorato.");
                continue;
            }

            Button[] buttons = ctrl.buttonsToClickCanvas.GetComponentsInChildren<Button>(true);

            if (buttons == null || buttons.Length == 0)
            {
                Debug.LogWarning($"[FeedbackButtonClickedCondition] Nessun Button trovato in '{ctrl.name}', ignorato.");
                continue;
            }

            pendingControllers.Add(ctrl);

            foreach (var btn in buttons)
            {
                buttonToController[btn] = ctrl;
                btn.onClick.AddListener(() => OnButtonClicked(ctrl));
                Debug.Log($"[FeedbackButtonClickedCondition] Listener registrato su '{btn.name}' per '{ctrl.name}'.");
            }
        }

        if (pendingControllers.Count == 0)
        {
            Debug.Log("[FeedbackButtonClickedCondition] Nessun controller richiede bottone. Condizione completata automaticamente.");
            Data.IsCompleted = true;
        }
    }

        private void OnButtonClicked(FeedbackPrefabController ctrl)
        {
            if (!pendingControllers.Contains(ctrl)) return;

            completedControllers.Add(ctrl);
            pendingControllers.Remove(ctrl);
            Debug.Log($"[FeedbackButtonClickedCondition] '{ctrl.name}' confermato. Rimangono {pendingControllers.Count} feedback in attesa.");

            if (pendingControllers.Count == 0)
            {
                Debug.Log("[FeedbackButtonClickedCondition] Tutti i feedback confermati → condizione completata.");
                Data.IsCompleted = true;
            }
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
                yield return null;
        }

        public override void End()
        {
            foreach (var (btn, _) in buttonToController)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }

            buttonToController.Clear();
            pendingControllers.Clear();
            completedControllers.Clear();
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }
}