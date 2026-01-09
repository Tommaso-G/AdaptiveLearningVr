using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using TMPro;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.SceneObjects;

[DataContract(IsReference = true)]
public class ShowPerformanceRecapBehaviour : Behavior<ShowPerformanceRecapBehaviour.EntityData>
{
    [DataContract(IsReference = true)]
    public class EntityData : IBehaviorData
    {
        [DisplayName("Posizione (Scene Object)")]
        [DataMember]
        public SingleSceneObjectReference recapPosition { get; set; } = new SingleSceneObjectReference();

        [DisplayName("Mostra davanti all'utente (ignora posizione)")]
        [DataMember]
        public bool showInFrontOfCamera { get; set; } = true;

        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        [IgnoreDataMember]
        [HideInProcessInspector]
        public string Name => "Mostra Feedback Recap";
    }

    public override IStageProcess GetActivatingProcess() => new ShowRecapProcess(Data);

    // -----------------------------------------------------------------
    private class ShowRecapProcess : StageProcess<EntityData>
    {
        public ShowRecapProcess(EntityData data) : base(data) { }

        public override void Start()
        {
            ShowRecapFeedback();
        }

        public override IEnumerator Update() { yield return null; }
        public override void End() { }
        public override void FastForward() { }

        // -----------------------------------------------------------------
        private void ShowRecapFeedback()
        {
            // 1️⃣ Trova il PerformanceManager
            PerformanceManager manager = Object.FindFirstObjectByType<PerformanceManager>();
            if (manager == null)
            {
                Debug.LogWarning("[ShowPerformanceRecapBehaviour] Nessun PerformanceManager trovato in scena.");
                return;
            }

            // 2️⃣ Verifica che il prefab sia assegnato
            GameObject prefab = manager.FeedbackRecapprefab;
            if (prefab == null)
            {
                Debug.LogWarning("[ShowPerformanceRecapBehaviour] Nessun prefab di recap assegnato al PerformanceManager.");
                return;
            }

            // 3️⃣ Istanzia il prefab
            GameObject instance = Object.Instantiate(prefab);
            instance.name = "FeedbackRecapInstance";

            // 4️⃣ Posizionamento: davanti alla camera oppure alla posizione indicata
            if (Data.showInFrontOfCamera)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 pos = cam.transform.position + cam.transform.forward * 1.2f;
                    instance.transform.position = pos;
                    instance.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
                }
                else
                {
                    Debug.LogWarning("[ShowPerformanceRecapBehaviour] Nessuna Camera trovata — posiziono in origine.");
                    instance.transform.position = Vector3.zero;
                }
            }
            else
            {
                Transform target = Data.recapPosition.Value?.GameObject?.transform;
                if (target != null)
                {
                    instance.transform.position = target.position;
                    instance.transform.rotation = target.rotation;
                }
                else
                {
                    Debug.LogWarning("[ShowPerformanceRecapBehaviour] Nessuna posizione assegnata al recap, uso origine.");
                    instance.transform.position = Vector3.zero;
                }
            }

            // 5️⃣ Trova *esattamente* l’oggetto chiamato "Modal Content Text"
            Transform modalTransform = instance.transform.Find("Modal Content Text");
            if (modalTransform == null)
            {
                Debug.LogWarning("[ShowPerformanceRecapBehaviour] Oggetto 'Modal Content Text' non trovato nel prefab!");
                return;
            }

            TextMeshProUGUI contentText = modalTransform.GetComponent<TextMeshProUGUI>();
            if (contentText == null)
            {
                Debug.LogWarning("[ShowPerformanceRecapBehaviour] 'Modal Content Text' non ha un componente TextMeshProUGUI!");
                return;
            }

            // 6️⃣ Ottieni il testo di recap dal manager e assegnalo
            contentText.text = manager.GetChapterRecapText();

            Debug.Log("[ShowPerformanceRecapBehaviour] Feedback recap mostrato con successo.");
        }
    }
}
