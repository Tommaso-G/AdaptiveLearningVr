using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class PlayAudioBehaviour : Behavior<PlayAudioBehaviour.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [DataMember]
        [DisplayName("Audio Source")]
        public SingleSceneObjectReference AudioSourceObject { get; set; } = new SingleSceneObjectReference();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Play Audio";
    }

    public override IStageProcess GetActivatingProcess() => new PlayAudioProcess(Data);

    // -------- PROCESS --------
    private class PlayAudioProcess : StageProcess<EntityData>
    {
        public PlayAudioProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            if (Data.AudioSourceObject?.Value == null)
            {
                Debug.LogWarning("[PlayAudio] Nessun oggetto AudioSource assegnato.");
                return;
            }

            AudioSource audioSource = Data.AudioSourceObject.Value.GameObject.GetComponent<AudioSource>();

            if (audioSource == null)
            {
                Debug.LogWarning($"[PlayAudio] Il GameObject '{Data.AudioSourceObject.Value.GameObject.name}' non ha un componente AudioSource.");
                return;
            }

            if (audioSource.clip == null)
            {
                Debug.LogWarning($"[PlayAudio] L'AudioSource su '{Data.AudioSourceObject.Value.GameObject.name}' non ha nessun clip assegnato.");
                return;
            }

            audioSource.Play();
            Debug.Log($"[PlayAudio] Play avviato su '{Data.AudioSourceObject.Value.GameObject.name}' - Clip: {audioSource.clip.name}");
        }

        public override IEnumerator Update() { yield break; }
        public override void End() { }
        public override void FastForward() { }
    }
}