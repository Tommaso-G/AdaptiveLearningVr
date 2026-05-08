using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    public class Breakable : MonoBehaviour
    {
        [Serializable]
        public class BreakEvent : UnityEvent<GameObject, GameObject> { }

        [Serializable]
        public class CollisionEvent : UnityEvent<Collision> { }

        [SerializeField]
        GameObject m_BrokenVersion;

        [SerializeField]
        string m_ColliderTag = "Destroyer";

        [SerializeField]
        BreakEvent m_OnBreak = new BreakEvent();

        public CollisionEvent OnCollisionEnterEvent;


        bool m_Destroyed = false;

        public BreakEvent onBreak => m_OnBreak;

        void OnCollisionEnter(Collision collision)
        {
            if (m_Destroyed)
                return;

            if (collision.gameObject.tag.Equals(
                m_ColliderTag,
                StringComparison.InvariantCultureIgnoreCase))
            {
                OnCollisionEnterEvent?.Invoke(collision);

                m_Destroyed = true;

                GameObject brokenVersion = Instantiate(
                    m_BrokenVersion,
                    transform.position,
                    transform.rotation
                );

                // passa riferimento originale
                Unbreakable unbreakable =
                    brokenVersion.GetComponent<Unbreakable>();

                if (unbreakable != null)
                {
                    unbreakable.originalObject = gameObject;
                }

                m_OnBreak.Invoke(collision.gameObject, brokenVersion);

                // DISATTIVA invece di distruggere
                gameObject.SetActive(false);
            }
        }

        public void ResetBreakable()
        {
            m_Destroyed = false;
            gameObject.SetActive(true);
        }
    }
}