using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    public class Unbreakable : MonoBehaviour
    {
        [Serializable]
        public class RestoreEvent : UnityEvent<GameObject> { }

        [SerializeField]
        float m_RestTime = 1.0f;

        [SerializeField]
        float m_RestoreTime = 2.0f;

        [SerializeField]
        RestoreEvent m_OnRestore = new RestoreEvent();

        // riferimento all'oggetto originale
        [HideInInspector]
        public GameObject originalObject;

        bool m_Resting = true;
        float m_Timer = 0.0f;
        bool m_Restored = false;

        struct ChildPoses
        {
            internal Pose m_StartPose;
            internal Pose m_EndPose;
        }

        Dictionary<Transform, ChildPoses> m_ChildPoses =
            new Dictionary<Transform, ChildPoses>();

        List<Transform> m_ChildTransforms =
            new List<Transform>();

        public RestoreEvent onRestore => m_OnRestore;

        void Start()
        {
            GetComponentsInChildren(m_ChildTransforms);

            foreach (var child in m_ChildTransforms)
            {
                m_ChildPoses.Add(
                    child,
                    new ChildPoses
                    {
                        m_StartPose =
                            new Pose(child.position, child.rotation)
                    }
                );
            }
        }

        void Update()
        {
            if (m_Restored)
                return;

            m_Timer += Time.deltaTime;

            // fase attesa
            if (m_Resting)
            {
                if (m_Timer > m_RestTime)
                {
                    m_Timer = 0.0f;
                    m_Resting = false;

                    foreach (var child in m_ChildTransforms)
                    {
                        if (child == null)
                            continue;

                        var poses = m_ChildPoses[child];

                        poses.m_EndPose =
                            new Pose(child.position, child.rotation);

                        m_ChildPoses[child] = poses;
                    }
                }
            }
            else
            {
                var timePercent = m_Timer / m_RestoreTime;

                if (timePercent > 1.0f)
                {
                    m_Restored = true;

                    // RIATTIVA ORIGINALE
                    if (originalObject != null)
                    {
                        originalObject.SetActive(true);

                        Breakable breakable =
                            originalObject.GetComponent<Breakable>();

                        if (breakable != null)
                        {
                            breakable.ResetBreakable();
                        }

                        m_OnRestore.Invoke(originalObject);
                    }

                    // distruggi broken version
                    Destroy(gameObject);
                }
                else
                {
                    timePercent =
                        1.0f - (
                            (1.0f - timePercent)
                            * (1.0f - timePercent)
                        );

                    foreach (var child in m_ChildTransforms)
                    {
                        if (child == null)
                            continue;

                        var poses = m_ChildPoses[child];

                        var lerpedPosition =
                            Vector3.Lerp(
                                poses.m_EndPose.position,
                                poses.m_StartPose.position,
                                timePercent
                            );

                        var lerpedRotation =
                            Quaternion.Slerp(
                                poses.m_EndPose.rotation,
                                poses.m_StartPose.rotation,
                                timePercent
                            );

                        child.position = lerpedPosition;
                        child.rotation = lerpedRotation;
                    }
                }
            }
        }
    }
}