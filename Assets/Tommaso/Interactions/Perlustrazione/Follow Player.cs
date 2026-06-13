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

    [Header("Timer")]
    public float timeLimit = 30f;
    private float _elapsedTime = 0f;
    private bool _timerActive = false;
    private bool _timerExpired = false;
    public bool TimerActive => _timerActive;

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

    public void StartTimer()
    {
        _timerActive = true;
        _elapsedTime = 0f;
        _timerExpired = false;
    }

    void Update()
    {
        if (!_hasAgent) return;

        if (_timerActive && !IsCompleted && !_timerExpired)
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= timeLimit)
            {
                HandleTimerExpired();
                return;
            }
        }

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

        if (_hasAgent && MyAnimator != null)
        {
            bool isMoving = agent.desiredVelocity.sqrMagnitude >= 0.01f;
            MyAnimator.SetBool("GoToRun", isMoving);
            MyAnimator.SetBool("GoToIdle", !isMoving);
        }
    }

    private void HandleTimerExpired()
    {
        if (IsCompleted) return;

        if (ErrorReporter != null)
        {
            ErrorReporter.RegisterError("Bambino_tempoScaduto");
        }
        else
        {
            Debug.LogError(
                "[FollowerAgentWithCheck] ErrorReporter non assegnato (timer scaduto)."
            );
        }

        _timerExpired = true;
        _timerActive = false;
        // Niente altro: l'NPC continua a seguire il player e lo step si completa normalmente
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

        _timerActive = false;

        bool isError = other != _bestCollider;

        if (!_hasAgent)
        {
            CompleteStep(isError);
            return;
        }

        Transform destination = matchedData.collider.transform.Find("destination");
        if (destination != null)
        {
            _currentDestination = destination;
            _reachedExit = true;
            _pendingError = isError;
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
        _timerActive = false;

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