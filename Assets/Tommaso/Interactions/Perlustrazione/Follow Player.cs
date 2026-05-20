using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using VRBuilder.Core;

public class FollowerAgentWithCheck : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    private NavMeshAgent agent;
    private bool _hasAgent = false;
    private Transform playerTransform;
    private bool isFollowing = false;

    public Animator MyAnimator;

    public List<Collider> destinationColliders = new List<Collider>();
    public string wrongStepName = "l'uscita non era la più vicina";

    private Collider _closestCollider;

    public ErrorReporter ErrorReporter;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            _hasAgent = true;
        }
        else
        {
            Debug.LogWarning($"[FollowerAgentWithCheck] [{name}] Nessun NavMeshAgent trovato — il movimento sarà disabilitato ma il controllo collider rimane attivo.");
        }
    }

    void Start()
    {
        ComputeClosestCollider();
    }

    void OnEnable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered += HandleFollowTriggered;
    }

    void OnDisable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered -= HandleFollowTriggered;
    }

    private void ComputeClosestCollider()
    {
        if (destinationColliders == null || destinationColliders.Count == 0) return;

        Collider closest = null;
        float minDist = float.MaxValue;

        foreach (Collider c in destinationColliders)
        {
            if (c == null) continue;

            ExitDoor exitDoor = c.GetComponentInChildren<ExitDoor>();
            if (exitDoor != null && exitDoor.blocked) continue;

            float dist = Vector3.Distance(transform.position, c.bounds.center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = c;
            }
        }

        _closestCollider = closest;
        Debug.Log($"[FollowerAgentWithCheck] [{name}] Collider più vicino all'avvio: {_closestCollider?.name}");
    }

    private void HandleFollowTriggered(GameObject follower, GameObject player)
    {
        if (follower != gameObject) return;

        playerTransform = player.transform;
        isFollowing = true;

        if (_hasAgent)
            MyAnimator.SetBool("GoToRun", true);
    }

    void Update()
    {
        if (isFollowing && playerTransform != null && _hasAgent)
            agent.SetDestination(playerTransform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCompleted) return;
        if (destinationColliders == null || destinationColliders.Count == 0) return;
        if (!destinationColliders.Contains(other)) return;

        ExitDoor exitDoor = other.GetComponentInChildren<ExitDoor>();

        if (exitDoor == null)
        {
            Debug.LogError($"[FollowerAgentWithCheck] [{name}] Nessuna ExitDoor trovata tra i figli di '{other.gameObject.name}'.");
            return;
        }

        if (exitDoor.blocked) return;

        IsCompleted = true;

        if (_closestCollider != null && other != _closestCollider)
        {
            if (ErrorReporter != null)
                ErrorReporter.RegisterError(gameObject.name + "_uscita");
            else
                Debug.LogError("[FollowerAgentWithCheck] ErrorReporter non assegnato.");
        }

        isFollowing = false;

        if (_hasAgent)
        {
            agent.ResetPath();
            MyAnimator.SetBool("GoToRun", false);
        }
    }
}