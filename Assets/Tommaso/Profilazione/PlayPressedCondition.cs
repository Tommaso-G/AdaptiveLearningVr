using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;

[DataContract(IsReference = true)]
public class PlayButtonClickedCondition : Condition<PlayButtonClickedCondition.PlayButtonClickedConditionData>
{
    public override IStageProcess GetActiveProcess()
    {
        return new PlayButtonClickedConditionActiveProcess(Data);
    }

    protected override IAutocompleter GetAutocompleter()
    {
        return new PlayButtonClickedConditionAutocompleter(Data);
    }

    [DataContract(IsReference = true)]
    [DisplayName("Play Button Clicked")]
    public class PlayButtonClickedConditionData : IConditionData
    {
        public Metadata Metadata { get; set; }

        public string Name { get; set; } = "Play Button Clicked";

        public bool IsCompleted { get; set; }
    }

    public class PlayButtonClickedConditionAutocompleter : Autocompleter<PlayButtonClickedConditionData>
    {
        public PlayButtonClickedConditionAutocompleter(PlayButtonClickedConditionData data)
            : base(data)
        {
        }

        public override void Complete()
        {
            Data.IsCompleted = true;
        }
    }

    public class PlayButtonClickedConditionActiveProcess : StageProcess<PlayButtonClickedConditionData>
    {
        private Button playButton;

        public PlayButtonClickedConditionActiveProcess(PlayButtonClickedConditionData data)
            : base(data)
        {
        }

        public override void Start()
        {
            // Trova anche oggetti DISATTIVATI
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (Button btn in buttons)
            {
                // Ignora prefab/assets fuori scena
                if (!btn.gameObject.scene.IsValid())
                    continue;

                if (btn.name == "Play")
                {
                    playButton = btn;
                    break;
                }
            }

            if (playButton == null)
            {
                Debug.LogWarning("[PlayButtonClickedCondition] Bottone 'Play' non trovato.");
                return;
            }

            Debug.Log("[PlayButtonClickedCondition] Bottone trovato: " + playButton.name);

            // Listener click
            playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            Debug.Log("[PlayButtonClickedCondition] Bottone Play cliccato.");

            Data.IsCompleted = true;
        }

        public override IEnumerator Update()
        {
            while (!Data.IsCompleted)
            {
                yield return null;
            }
        }

        public override void End()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        public override void FastForward()
        {
            Data.IsCompleted = true;
        }
    }
}