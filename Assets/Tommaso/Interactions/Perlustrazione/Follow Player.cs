using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using VRBuilder.Core;

public class FollowerAgentWithCheck : MonoBehaviour, ICompletableStep
{
    [System.Serializable]
    public class ExitColliderData
    {
        public Collider collider;
        public int priority;
    }

    public bool IsCompleted { get; private set; } = false;

    private NavMeshAgent agent;
    private bool _hasAgent = false;

    private Transform playerTransform;
    private bool isFollowing = false;

    public Animator MyAnimator;

    [Header("Collider uscite")]
    public List<ExitColliderData> exitColliders = new List<ExitColliderData>();

    public ErrorReporter ErrorReporter;

    private Collider _bestCollider;

    public bool IsFollowing => isFollowing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            _hasAgent = true;
        }
        else
        {
            Debug.LogWarning(
                $"[FollowerAgentWithCheck] [{name}] Nessun NavMeshAgent trovato."
            );
        }
    }

    void Start()
    {
        ComputeBestCollider();
    }

    void OnEnable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered += HandleFollowTriggered;
    }

    void OnDisable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered -= HandleFollowTriggered;
    }

    private void HandleFollowTriggered(GameObject follower, GameObject player)
    {
        if (follower != gameObject) return;

        playerTransform = player.transform;
        isFollowing = true;

        if (_hasAgent)
        {
            MyAnimator.SetBool("GoToRun", true);
        }
    }

    void Update()
    {
        if (isFollowing && playerTransform != null && _hasAgent)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    private void ComputeBestCollider()
    {
        _bestCollider = null;

        int bestPriority = int.MaxValue;

        foreach (ExitColliderData data in exitColliders)
        {
            if (data == null || data.collider == null)
                continue;

            ExitDoor exitDoor =
                data.collider.GetComponentInChildren<ExitDoor>();

            // Se blocked, ignoralo
            if (exitDoor != null && exitDoor.blocked)
                continue;

            // Cerca la priority più piccola
            if (data.priority < bestPriority)
            {
                bestPriority = data.priority;
                _bestCollider = data.collider;
            }
        }

        Debug.Log(
            $"[FollowerAgentWithCheck] [{name}] Collider corretto: {_bestCollider?.name}"
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCompleted) return;

        ExitColliderData matchedData = null;

        foreach (ExitColliderData data in exitColliders)
        {
            if (data.collider == other)
            {
                matchedData = data;
                break;
            }
        }

        if (matchedData == null)
            return;

        ExitDoor exitDoor =
            matchedData.collider.GetComponentInChildren<ExitDoor>();

        // Se blocked non fare nulla
        if (exitDoor != null && exitDoor.blocked)
            return;

        bool shouldRegisterError = other != _bestCollider;

        CompleteStep(shouldRegisterError);
    }

    private void CompleteStep(bool registerError)
    {
        IsCompleted = true;

        if (registerError)
        {
            if (ErrorReporter != null)
            {
                ErrorReporter.RegisterError(gameObject.name + "_uscita");
            }
            else
            {
                Debug.LogError(
                    "[FollowerAgentWithCheck] ErrorReporter non assegnato."
                );
            }
        }

        isFollowing = false;

        if (_hasAgent)
        {
            agent.ResetPath();
            MyAnimator.SetBool("GoToRun", false);
        }
    }
}