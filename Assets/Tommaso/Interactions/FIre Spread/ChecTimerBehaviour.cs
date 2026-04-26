using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;

[DataContract(IsReference = true)]
public class CheckTimerBehavior : Behavior<CheckTimerBehavior.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [DataMember]
        [DisplayName("Soglia tempo (secondi)")]
        public float TimeThreshold { get; set; } = 30f;

        [DataMember]
        [DisplayName("Nome oggetto interagito")]
        public string InteractedObjectName { get; set; } = "";

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Check Timer";
    }

    public override IStageProcess GetActivatingProcess() => new CheckTimerProcess(Data);

    private class CheckTimerProcess : StageProcess<EntityData>
    {
        public CheckTimerProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            // Recupera il ColliderTimer dalla scena
            ColliderTimer colliderTimer = Object.FindFirstObjectByType<ColliderTimer>();
            if (colliderTimer == null)
            {
                Debug.LogWarning("[CheckTimerBehavior] Nessun ColliderTimer trovato in scena.");
                return;
            }
            // Recupera il capitolo corrente dal processo attivo
            IProcess process = ProcessRunner.Current;
            string chapterName = "Capitolo sconosciuto";

            if (process != null && process.Data.Current != null)
                chapterName = process.Data.Current.Data.Name;

            // Il nome dello step corrente come "wrong step"
            string currentStepName = "Step sconosciuto";
            if (process?.Data.Current?.Data.Current != null)
                currentStepName = process.Data.Current.Data.Current.Data.Name;

            float elapsed = colliderTimer.GetTime();
            Debug.Log($"[CheckTimerBehavior] Tempo rilevato: {elapsed:F2}s | Soglia: {Data.TimeThreshold:F2}s");

            if (elapsed > Data.TimeThreshold)
            {
                ErrorEvent.OnError?.Invoke(chapterName, currentStepName, Data.InteractedObjectName);

                Debug.Log($"[CheckTimerBehavior] Soglia superata — errore registrato. " +
                          $"Capitolo: '{chapterName}' | Step: '{currentStepName}' | " +
                          $"Oggetto: '{Data.InteractedObjectName}' | Tempo: {elapsed:F2}s");
            }
            else
            {
                ChapterTracker.ChangeTime?.Invoke(chapterName, elapsed);
                Debug.Log("[CheckTimerBehavior] Tempo nella norma, nessun errore registrato.");
            }
        }

        public override IEnumerator Update() { yield break; }
        public override void End() { }
        public override void FastForward() { }
    }
}