using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRBuilder.Core.Properties;
using System.Collections.Generic;
using System.Linq;

public class ColliderPropertyInteractionSource : InteractionSource
{
    [SerializeField]
    private string requiredTag = "Player";
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<ColliderWithTriggerProperty>(out _);

    public List<FollowerAgentWithCheck> followAgents = new List<FollowerAgentWithCheck>();

    private void Awake()
    {
        errorString = "Aula";
        followAgents = FindObjectsByType<FollowerAgentWithCheck>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).ToList();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        foreach (var agent in followAgents)
        {
            if (agent.IsFollowing)
            {
                return;
            }
        }

        RaiseInteraction(
            InteractionKind.Trigger,
            gameObject,
            interactor: other.gameObject
        );
    }
}
