using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Rappresenta una maniglia (handle) XR che permette di afferrare e trascinare
    /// un oggetto fisico con componente XRPhysicsGrabObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GrabbableHandle : XRBaseInteractable
    {
        private XRPhysicsGrabObject m_Grabbable;

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnGrab);
            selectExited.AddListener(OnRelease);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(OnGrab);
            selectExited.RemoveListener(OnRelease);
            base.OnDisable();
        }

        void Start()
        {
            // Cerca un XRPhysicsGrabObject nel parent
            m_Grabbable = GetComponentInParent<XRPhysicsGrabObject>();
            if (m_Grabbable == null)
                Debug.LogWarning($"Nessun XRPhysicsGrabObject trovato nel parent di {name}!");
        }

        void OnGrab(SelectEnterEventArgs args)
        {
            if (m_Grabbable != null)
                m_Grabbable.BeginGrab(args);
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (m_Grabbable != null)
                m_Grabbable.EndGrab();
        }
    }
}
