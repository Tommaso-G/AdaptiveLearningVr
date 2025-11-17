using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    public class box : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Hinge joint fisico della scatola")]
        private HingeJoint m_boxJoint;

        [SerializeField]
        [Tooltip("Transform joint che permette di trascinare la scatola con l'interactor")]
        private TransformJoint m_boxPuller;

        private bool m_Closed = false;
        public bool IsClosed => m_Closed;

        void Start()
        {
            if (m_boxJoint == null)
            {
                Debug.LogWarning("Nessun HingeJoint assegnato alla scatola!");
                return;
            }


            m_Closed = false;
        }

        /// <summary>
        /// Attiva il TransformJoint quando la porta viene afferrata.
        /// </summary>
        public void BeginboxPulling(SelectEnterEventArgs args)
        {
            if (m_boxPuller == null)
            {
                Debug.LogWarning("Nessun TransformJoint assegnato alla scatola!");
                return;
            }

            var attachTransform = args.interactorObject.GetAttachTransform(args.interactableObject);
            if (attachTransform == null)
            {
                Debug.LogWarning("Nessun attachTransform trovato sull’interactor!");
                return;
            }

            m_boxPuller.connectedBody = attachTransform;
            m_boxPuller.enabled = true;
        }

        /// <summary>
        /// Disattiva il TransformJoint quando la porta viene rilasciata.
        /// </summary>
        public void EndboxPulling()
        {
            if (m_boxPuller == null)
                return;

            m_boxPuller.enabled = false;
            m_boxPuller.connectedBody = null;
        }
    }
}
