using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    public class ClosableDoor : Door
    {
       
        public bool IsClosed => m_Closed;


        public override void Start()
        {


            // Imposta i limiti di apertura tra 0° e 90°
            m_OpenDoorLimits = m_DoorJoint.limits;
            m_OpenDoorLimits.min = 0.0f;
            m_OpenDoorLimits.max = 90.0f;
            m_DoorJoint.limits = m_OpenDoorLimits;


            
            m_Closed = false;
        }

        void Awake()
        {
            m_DoorJoint.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }


        public override void Update()
        {
            // If the door is open, keep track of the hinge joint and see if it enters a state where it should close again
            if (!m_Closed)
            {
                if (m_LastHandleValue < m_HandleCloseValue)
                    return;

                if (Mathf.Abs(m_DoorJoint.angle) < m_HingeCloseAngle)
                {
                    m_DoorJoint.limits = m_ClosedDoorLimits;
                    m_Closed = true;


                }
            }

            if (m_KnobInteractor != null && m_KnobInteractorAttachTransform != null)
            {
                var distance = (m_KnobInteractorAttachTransform.position - m_KeyKnob.transform.position).magnitude;

                // If over threshold, break and grant the key back to the interactor
                if (distance > m_KeyPullDistance)
                {
                    var newKeyInteractor = m_KnobInteractor;
                    m_KeySocket.SetActive(true);
                    m_Key.transform.gameObject.SetActive(true);
                    newKeyInteractor.interactionManager.SelectEnter(newKeyInteractor, m_Key);
                    m_KeyKnob.SetActive(false);
                }
            }
        }







    }
}
