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
        [Header("Distanza fissa davanti al giocatore (metri)")]
        [Range(0.2f, 10f)]
        public float grabDistance = 0.6f;

        private readonly List<XR.Interaction.Toolkit.Interactors.IXRSelectInteractor> grabbingHands = new();

        private float m_InitialY;
        private float m_InitialRotX;
        private Rigidbody f_rigidbodyRef;

        [HideInInspector, JsonIgnore]
        public bool IsGrabbed = false;

        protected override void Awake()
        {
            base.Awake();
            f_rigidbodyRef = GetComponent<Rigidbody>();
            selectMode = InteractableSelectMode.Multiple;
            movementType = MovementType.Instantaneous;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (!grabbingHands.Contains(args.interactorObject))
                grabbingHands.Add(args.interactorObject);

            IsGrabbed = grabbingHands.Count > 0;

            if (IsGrabbed)
            {
                // Disattiva la fisica e il tracking
                f_rigidbodyRef.isKinematic = true;
                trackPosition = trackRotation = false;

                m_InitialY = transform.position.y;
                m_InitialRotX = transform.localEulerAngles.x;
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            grabbingHands.Remove(args.interactorObject);
            IsGrabbed = grabbingHands.Count > 0;

            if (!IsGrabbed)
            {
                // Ripristina fisica e tracking
                f_rigidbodyRef.isKinematic = false;
                trackPosition = trackRotation = true;
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (!IsGrabbed) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // --- Mantieni posizione centrata davanti alla camera ---
            Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance + cam.transform.right * 0.4f;
            targetPos.y = m_InitialY;  // Mantiene la stessa altezza di quando è stato afferrato
            transform.position = targetPos;

            // --- Allinea la rotazione per essere centrato rispetto alla camera ---
            // Guarda nella direzione opposta al visore (frontale e centrato)
            Vector3 lookDir = -cam.transform.forward;
            lookDir.y = 0; // Mantiene l’orientamento orizzontale
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);

            // --- Blocca rotazione X (mantiene inclinazione iniziale) ---
            Vector3 euler = transform.localEulerAngles;
            euler.x = m_InitialRotX;
            transform.localEulerAngles = euler;
        }

    }
}
