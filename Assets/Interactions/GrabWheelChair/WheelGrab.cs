using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class XRGrabNoY : XRGrabInteractable
    {
        [Header("Attach Transforms personalizzati")]
        public Transform AttachTransformA;
        public Transform AttachTransformB;

        [Header("Distanza fissa davanti al giocatore (metri)")]
        [Range(0.2f, 10f)]
        public float grabDistance = 0.6f;

        private Transform primaryAttachUsed;
        private Transform secondaryAttachUsed;

        private readonly List<XR.Interaction.Toolkit.Interactors.IXRSelectInteractor> grabbingHands = new();

        private float m_InitialY;
        private float m_InitialRotX;
        private Vector3 m_OriginalPosition;
        private Quaternion m_OriginalRotation;

        [HideInInspector]
        [JsonIgnore]
        public bool IsGrabbed = false;

        protected override void Awake()
        {
            base.Awake();
            selectMode = InteractableSelectMode.Multiple;
            movementType = MovementType.Instantaneous;
            trackPosition = true;
            trackRotation = true;

            m_OriginalPosition = transform.position;
            m_OriginalRotation = transform.rotation;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (!grabbingHands.Contains(args.interactorObject))
                grabbingHands.Add(args.interactorObject);

            IsGrabbed = grabbingHands.Count > 0;

            // Prima mano → decide quale attach è più vicino
            if (grabbingHands.Count == 1)
            {
                var hand = args.interactorObject.transform;
                float distA = Vector3.Distance(hand.position, AttachTransformA.position);
                float distB = Vector3.Distance(hand.position, AttachTransformB.position);

                primaryAttachUsed = (distA <= distB) ? AttachTransformA : AttachTransformB;
                secondaryAttachUsed = (primaryAttachUsed == AttachTransformA) ? AttachTransformB : AttachTransformA;
                attachTransform = primaryAttachUsed;

                m_InitialY = transform.position.y;
                m_InitialRotX = transform.localEulerAngles.x;
            }
            else if (grabbingHands.Count == 2)
            {
                attachTransform = secondaryAttachUsed;
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            grabbingHands.Remove(args.interactorObject);
            IsGrabbed = grabbingHands.Count > 0;

            if (grabbingHands.Count == 0)
            {
                primaryAttachUsed = null;
                secondaryAttachUsed = null;

                // Mantiene la posizione dove lasci l’oggetto ma resetta le variabili
                m_InitialY = transform.position.y;
                m_InitialRotX = transform.localEulerAngles.x;
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (!IsGrabbed)
                return;

            MantieniDavantiAlGiocatore();
            BloccaYERotazioneX();
            MantieniOrientamentoOppostoGiocatore();
        }

        private void BloccaYERotazioneX()
        {
            Vector3 pos = transform.position;
            pos.y = m_InitialY;
            transform.position = pos;

            Vector3 localRot = transform.localEulerAngles;
            localRot.x = m_InitialRotX;
            transform.localEulerAngles = localRot;
        }

        private void MantieniOrientamentoOppostoGiocatore()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
                return;

            Vector3 cameraPos = mainCam.transform.position;
            Vector3 directionToCamera = (cameraPos - transform.position).normalized;

            // Guarda nella direzione opposta alla camera
            Vector3 lookTarget = transform.position + directionToCamera;
            lookTarget.y = transform.position.y;

            transform.LookAt(lookTarget);

            Vector3 euler = transform.localEulerAngles;
            euler.x = m_InitialRotX;
            transform.localEulerAngles = euler;
        }

        /// <summary>
        /// Mantiene l’oggetto sempre davanti al giocatore, alla distanza fissa specificata.
        /// </summary>
        private void MantieniDavantiAlGiocatore()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
                return;

            // Calcola posizione davanti al visore
            Vector3 targetPos = mainCam.transform.position + mainCam.transform.forward * grabDistance;

            // Mantiene altezza iniziale (non sale)
            targetPos.y = m_InitialY;

            transform.position = targetPos;
        }
    }
}
