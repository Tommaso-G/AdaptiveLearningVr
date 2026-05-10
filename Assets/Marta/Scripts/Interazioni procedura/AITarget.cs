using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AITarget : MonoBehaviour
{
    public Transform Target;

    private NavMeshAgent m_Agent;
    private Animator m_Animator;
    [SerializeField] private float m_Distance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponentInChildren<Animator>(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Target == null)
            return;

        if (Vector3.Distance(m_Agent.transform.position, Target.position) < m_Distance)
        {
            resetTarget();
        }
        else
        {
            m_Agent.SetDestination(Target.position);
        }

    }

    public void setTarget(Transform target)
    {
        if (target.gameObject.activeSelf)
        {
            Target = target;
            m_Animator.SetBool("GoToRun", true);
        }
    }

    public void resetTarget()
    {
        Target = null;
        m_Agent.ResetPath();
        m_Animator.SetBool("GoToRun", false);
        this.gameObject.SetActive(false);
    }

    private void OnAnimatorMove()
    {
        if (m_Animator.GetBool("GoToRun") == true)
        {
            m_Agent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
        }
    }
}
