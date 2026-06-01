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
    private Transform _currentDestination = null;
    private bool _reachedExit = false;
    private bool _pendingError = false;

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
        if (!_hasAgent) return;

        if (_reachedExit && _currentDestination != null)
        {
            agent.SetDestination(_currentDestination.position);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                CompleteStep(_pendingError);
                _reachedExit = false;
                _currentDestination = null;
            }
        }
        else if (isFollowing && playerTransform != null)
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

            ExitDoor exitDoor = data.collider.GetComponentInChildren<ExitDoor>();

            if (exitDoor != null && exitDoor.blocked)
                continue;

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
        if (_reachedExit) return;

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

        ExitDoor exitDoor = matchedData.collider.GetComponentInChildren<ExitDoor>();

        if (exitDoor != null && exitDoor.blocked)
            return;

        // Prima cosa: chiama Toggle su tutti gli AngleTransitioner
       // AngleTransitioner[] transitioners = matchedData.collider.GetComponentsInChildren<AngleTransitioner>();
        //foreach (AngleTransitioner t in transitioners)
        //{
           // t.Toggle();
        //}

        Transform destination = matchedData.collider.transform.Find("destination");
        if (destination != null)
        {
            _currentDestination = destination;
            _reachedExit = true;
            _pendingError = other != _bestCollider;
            isFollowing = false;
            Debug.Log($"[FollowerAgentWithCheck] [{name}] Destinazione finale: {destination.name}");
        }
        else
        {
            Debug.LogWarning($"[FollowerAgentWithCheck] [{name}] Nessun figlio 'destination' trovato in {matchedData.collider.name}");
        }
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