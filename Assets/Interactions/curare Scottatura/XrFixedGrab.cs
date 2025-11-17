using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Permette di afferrare e trascinare un qualsiasi oggetto fisico con Rigidbody
    /// utilizzando un TransformJoint durante l'interazione XR.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class XRPhysicsGrabObject : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField]
        [Tooltip("TransformJoint usato per collegare temporaneamente l'interactor all'oggetto.")]
        private TransformJoint m_ObjectPuller;

        private Rigidbody m_Rigidbody;

        // Stato di interazione
        private bool m_IsGrabbed = false;
        public bool IsGrabbed => m_IsGrabbed;

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            if (m_Rigidbody == null)
                Debug.LogError("Nessun Rigidbody trovato su " + name);

            if (m_ObjectPuller != null)
                m_ObjectPuller.enabled = false;
        }

        /// <summary>
        /// Attiva il TransformJoint quando l'oggetto viene afferrato.
        /// </summary>
        public void BeginGrab(SelectEnterEventArgs args)
        {
            if (m_ObjectPuller == null)
            {
                Debug.LogWarning("Nessun TransformJoint assegnato all'oggetto!");
                return;
            }

            var attachTransform = args.interactorObject.GetAttachTransform(args.interactableObject);
            if (attachTransform == null)
            {
                Debug.LogWarning("Nessun attachTransform trovato sull’interactor!");
                return;
            }

            // Collega l’interactor al corpo fisico
            m_ObjectPuller.connectedBody = attachTransform;
            m_ObjectPuller.enabled = true;

            // Imposta alcune proprietà fisiche per evitare comportamenti strani
            m_Rigidbody.linearDamping = 2f;
            m_Rigidbody.angularDamping = 2f;

            m_IsGrabbed = true;
        }

        /// <summary>
        /// Disattiva il TransformJoint quando l'oggetto viene rilasciato.
        /// </summary>
        public void EndGrab()
        {
            if (m_ObjectPuller == null)
                return;

            m_ObjectPuller.enabled = false;
            m_ObjectPuller.connectedBody = null;

            m_Rigidbody.linearDamping = 0f;
            m_Rigidbody.angularDamping= 0.05f;

            m_IsGrabbed = false;
        }

        /// <summary>
        /// Debug: mostra il centro di massa e la connessione del joint.
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (m_ObjectPuller != null && m_ObjectPuller.enabled && m_ObjectPuller.connectedBody != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, m_ObjectPuller.connectedBody.transform.position);
                Gizmos.DrawSphere(m_ObjectPuller.connectedBody.transform.position, 0.02f);
            }
        }
    }
}
