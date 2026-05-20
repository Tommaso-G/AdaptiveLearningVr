using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Invia tutti gli agenti NavMesh verso un collider scelto.
/// Gli agenti si distruggono autonomamente al contatto (vedi AgentSelfDestruct).
/// </summary>
public class NavAgentDestinationSetter : MonoBehaviour
{
    [Header("Agenti")]
    public List<NavMeshAgent> agents = new List<NavMeshAgent>();

    [Header("Destinazione")]
    public Collider targetCollider;

    private void Start()
    {
        agents.RemoveAll(a => a == null);

    }

    [ContextMenu("Invia Agenti alla Destinazione")]
    public void SendAgentsToTarget()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("[NavAgentSetter] Nessun targetCollider assegnato!");
            return;
        }

        foreach (NavMeshAgent agent in agents)
        {
            if (agent == null) continue;

            // Aggiungi il componente autodistruzione e passagli il collider target
            AgentSelfDestruct sd = agent.gameObject.GetComponent<AgentSelfDestruct>();
            if (sd == null) sd = agent.gameObject.AddComponent<AgentSelfDestruct>();
            sd.targetCollider = targetCollider;

            agent.SetDestination(targetCollider.bounds.center);
        }
    }
}