using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavAgentDestinationSetter : MonoBehaviour
{
    [Header("Agenti")]
    public List<NavMeshAgent> agents = new List<NavMeshAgent>();

    [Header("Destinazione")]
    public Collider targetCollider;

    [Header("Porta")]
    public ExitDoor exitDoor;
    public AngleTransitioner doorTransitioner;
    public ExitDoor exitDoor2;
    public AngleTransitioner doorTransitioner2;

    private int _activeAgents;
    private bool _doorOpen = false;

    private void Start()
    {
        agents.RemoveAll(a => a == null);
    }

    private void Update()
    {
        if (_doorOpen)
        {
            int alive = 0;
            foreach (NavMeshAgent agent in agents)
                if (agent != null && agent.gameObject.activeInHierarchy) alive++;

            if (alive == 0)
                CloseDoor();
        }
    }

    [ContextMenu("Invia Agenti alla Destinazione")]
    public void SendAgentsToTarget()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("[NavAgentSetter] Nessun targetCollider assegnato!");
            return;
        }

        // Controlla che la destinazione sia attiva
        if (!targetCollider.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[NavAgentSetter] Il targetCollider è inattivo, operazione annullata.");
            return;
        }

        foreach (NavMeshAgent agent in agents)
        {
            if (agent == null) continue;

            // Salta gli agenti inattivi
            if (!agent.gameObject.activeInHierarchy) continue;

            AgentSelfDestruct sd = agent.gameObject.GetComponent<AgentSelfDestruct>();
            if (sd == null) sd = agent.gameObject.AddComponent<AgentSelfDestruct>();
            sd.targetCollider = targetCollider;

            Animator anim = agent.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("GoToReact");
                StartCoroutine(SetGoToWalkDelayed(anim, agent, 4f));
            }
            else
            {
                agent.SetDestination(targetCollider.bounds.center);
                OpenDoor();
            }
        }
    }

    private IEnumerator SetGoToWalkDelayed(Animator anim, NavMeshAgent agent, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (agent != null && agent.gameObject.activeInHierarchy && targetCollider != null && targetCollider.gameObject.activeInHierarchy)
        {
            agent.SetDestination(targetCollider.bounds.center);
            OpenDoor();
        }

        yield return new WaitForSeconds(0.5f);

        if (anim != null && anim.gameObject.activeInHierarchy)
            anim.SetTrigger("GoToWalk");
    }

    private void OpenDoor()
    {
        if (_doorOpen) return;
        _doorOpen = true;

        if (exitDoor != null) exitDoor.isBlock(false);
        if (exitDoor2 != null) exitDoor2.isBlock(false);

        if (doorTransitioner != null) doorTransitioner.Toggle();
        if (doorTransitioner2 != null) doorTransitioner2.Toggle();
    }

    private void CloseDoor()
    {
        if (!_doorOpen) return;
        _doorOpen = false;

        if (doorTransitioner != null) doorTransitioner.Toggle();
        if (doorTransitioner2 != null) doorTransitioner2.Toggle();

        if (exitDoor != null)
        {
            exitDoor.isBlock(true);
            StartCoroutine(UnblockAfter(exitDoor, doorTransitioner));
        }
        if (exitDoor2 != null)
        {
            exitDoor2.isBlock(true);
            StartCoroutine(UnblockAfter(exitDoor2, doorTransitioner2));
        }
    }

    private IEnumerator UnblockAfter(ExitDoor door, AngleTransitioner transitioner)
    {
        float waitTime = transitioner != null ? transitioner.duration : 0f;
        yield return new WaitForSeconds(waitTime);

        if (door != null)
            door.isBlock(false);
    }
}