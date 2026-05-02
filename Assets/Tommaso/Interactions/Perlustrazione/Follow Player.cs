using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using VRBuilder.Core;

public class FollowerAgentWithCheck : MonoBehaviour, ICompletableStep
{
    // ── ICompletableStep ──────────────────────────────────────────
    public bool IsCompleted { get; private set; } = false;

    // ── NavMesh / Follow ──────────────────────────────────────────
    private NavMeshAgent agent;
    private bool _hasAgent = false;
    private Transform playerTransform;
    private bool isFollowing = false;

    public Animator MyAnimator;

    // ── Collider check ────────────────────────────────────────────
    [Tooltip("Lista di collider — l'NPC deve entrare nel più vicino")]
    public List<Collider> destinationColliders = new List<Collider>();

    [Tooltip("Nome dello step sbagliato da passare all'ErrorEvent")]
    public string wrongStepName = "l'uscita non era la più vicina";

    private Collider _closestCollider;

    public ErrorReporter ErrorReporter;

    // ─────────────────────────────────────────────────────────────
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
        Debug.Log($"[FollowerAgentWithCheck] [{name}] OnEnable — sottoscrizione all'evento");
    }

    void OnDisable()
    {
        FollowPlayerBehavior.OnFollowPlayerTriggered -= HandleFollowTriggered;
    }

    // ── Calcola il collider più vicino alla posizione corrente dell'NPC ──
    private void ComputeClosestCollider()
    {
        if (destinationColliders == null || destinationColliders.Count == 0) return;

        Collider closest = null;
        float minDist = float.MaxValue;

        foreach (Collider c in destinationColliders)
        {
            if (c == null) continue;
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

    // ── Riceve l'evento di follow ─────────────────────────────────
    private void HandleFollowTriggered(GameObject follower, GameObject player)
    {
        Debug.Log($"[FollowerAgentWithCheck] [{name}] Evento ricevuto — follower: {follower?.name}, atteso: {name}, match: {follower == gameObject}");

        if (follower != gameObject) return;

        Debug.Log($"[FollowerAgentWithCheck] [{name}] inizia a seguire {player.name}");
        playerTransform = player.transform;
        isFollowing = true;

        if (_hasAgent)
            MyAnimator.SetBool("GoToRun", true);
    }

    // ── Segue il player ogni frame ────────────────────────────────
    void Update()
    {
        if (isFollowing && playerTransform != null && _hasAgent)
            agent.SetDestination(playerTransform.position);
    }

    // ── Controlla l'ingresso nei collider ─────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (IsCompleted) return;
        if (destinationColliders == null || destinationColliders.Count == 0) return;
        if (!destinationColliders.Contains(other)) return;

        string chapterName = ProcessRunner.Current?.Data.Current?.Data.Name ?? "Unknown Chapter";

        if (other == _closestCollider)
        {
            Debug.Log($"[FollowerAgentWithCheck] [{name}] Collider corretto: {other.name}");
        }
        else
        {
            Debug.Log($"[FollowerAgentWithCheck] [{name}] Collider sbagliato: '{other.name}', più vicino era '{_closestCollider?.name}'");

            if (ErrorReporter != null)
            {
                ErrorReporter.RegisterError(gameObject.name);
            }
            else
            {
                Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
            }
        }

        // In entrambi i casi: step completato e movimento fermato
        IsCompleted = true;
        isFollowing = false;

        if (_hasAgent)
        {
            agent.ResetPath();
            MyAnimator.SetBool("GoToRun", false);
        }
    }
}