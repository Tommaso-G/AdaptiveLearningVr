using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    public class ClosableDoor : Door
    {
        public UnityEvent onDoorClosed;
        public UnityEvent onDoorOpened;
        public bool IsClosed => m_Closed;
        private bool was_closed = false;

        public bool isPermanentlyClosed = false;

        public bool IsGrabbed = false;

        public bool simulateHandle = false;

        [Header("Door Initial State")]
        public bool startsOpen = true;

        public float closed_angle = 85f;
        public float opened_angle = 4f;
        public float initialSpring = 100f;
        public float initalDamper = 35f;

        private Rigidbody m_Rigidbody;
        private Quaternion initialRotation;

        public override void Start()
        {
            m_OpenDoorLimits = m_DoorJoint.limits;
            m_OpenDoorLimits.min = 0.0f;
            m_OpenDoorLimits.max = 90.0f;
            m_DoorJoint.limits = m_OpenDoorLimits;

            if (startsOpen)
            {
                m_DoorJoint.transform.localRotation = initialRotation;
                m_Rigidbody.isKinematic = true;
                m_Closed = false;
                was_closed = false;
            }
            else
            {
                m_DoorJoint.transform.localRotation = initialRotation * Quaternion.Euler(0f, -90f, 0f);
                m_Rigidbody.isKinematic = true;
                m_Closed = true;
                was_closed = true;
            }
        }

        public void CloseFromCode(bool toOpen)
        {
            m_Closed = toOpen;
        }

        void Awake()
        {
            m_DoorJoint.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            m_Rigidbody = m_DoorJoint.GetComponent<Rigidbody>();
            initialRotation = m_DoorJoint.transform.localRotation;
        }

        public void ActivateIsGrabbed(bool Activate)
        {
            if (isPermanentlyClosed) return;

            if (Activate)
            {
                IsGrabbed = true;
                m_Rigidbody.isKinematic = false;
            }
            else IsGrabbed = false;
        }

        public override void Update()
        {
            if (simulateHandle) return;

            float currentAngle = Quaternion.Angle(initialRotation, m_DoorJoint.transform.localRotation);

            Rigidbody rb = m_DoorJoint.GetComponent<Rigidbody>();

            // Porta chiusa
            if (!m_Closed && currentAngle >= closed_angle && !IsGrabbed)
            {
                Debug.Log("Update !m_Closed && currentAngle >= closed_angle && !IsGrabbed");
                m_Closed = true;
                was_closed = true;
                m_Rigidbody.angularVelocity = Vector3.zero;
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_DoorJoint.transform.localRotation = initialRotation * Quaternion.Euler(0f, -90f, 0f);
                m_Rigidbody.isKinematic = true;
                onDoorClosed?.Invoke();
            }

            // Porta aperta
            else if (m_Closed && currentAngle <= closed_angle && !isPermanentlyClosed)
            {
                Debug.Log("Update m_Closed && currentAngle <= closed_angle && !isPermanentlyClosed");
                m_Closed = false;
                onDoorOpened?.Invoke();
            }

            // Porta spalancata
            if (was_closed && currentAngle <= opened_angle && !IsGrabbed)
            {
                m_Rigidbody.isKinematic = true;
                was_closed = false;
            }
        }

        public void ActivateSpring()
        {
            m_DoorJoint.useSpring = true;
            JointSpring spring = m_DoorJoint.spring;
            spring.spring = initialSpring;
            spring.damper = initalDamper;

            if (was_closed)
            {
                spring.targetPosition = 90f;
                m_DoorJoint.spring = spring;
            }
            else
            {
                spring.targetPosition = 0f;
                m_DoorJoint.spring = spring;
            }
        }

        public void OpenWithSpring()
        {
            isPermanentlyClosed = false;

            m_Rigidbody.isKinematic = false;
            was_closed = true;
            Debug.Log("Aperta porta con OpenWithSPring");
            m_DoorJoint.useSpring = true;
            JointSpring spring = m_DoorJoint.spring;
            spring.spring = initialSpring;
            spring.damper = initalDamper;
            spring.targetPosition = 90f;
            m_DoorJoint.spring = spring;
        }

        public void ClosePermanently()
        {
            isPermanentlyClosed = true;

            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_DoorJoint.transform.localRotation = initialRotation * Quaternion.Euler(0f, -90f, 0f);
            m_Rigidbody.isKinematic = true;
            m_Closed = true;
            was_closed = true;

            onDoorClosed?.Invoke();
        }

        public void SimulateHandle()
        {
            m_Closed = !m_Closed;
            if (m_Closed) onDoorClosed?.Invoke();
        }

        public void DisableInteractionListener()
        {
            InteractionListener interactionListener = GetComponent<InteractionListener>();
            if (interactionListener != null)
            {
                interactionListener.enabled = false;
            }
        }
    }
}