using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class FollowerAgentInteractionSource : InteractionSource
{
    private FollowerAgentWithCheck agent;
    public override bool CanHandle(GameObject obj)
        => obj.TryGetComponent<FollowerAgentWithCheck>(out _);
    private void Awake()
    {
        agent = GetComponent<FollowerAgentWithCheck>();
        errorString = "Bambino";
    }

    private void OnEnable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered += OnFollowPlayer;
    }

    private void OnDisable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered -= OnFollowPlayer;
    }

    private void OnFollowPlayer(GameObject follower, GameObject player)
    {
        if (follower != gameObject)
            return;

        RaiseInteraction(
            InteractionKind.NPC,
            follower
        );
    }
}
