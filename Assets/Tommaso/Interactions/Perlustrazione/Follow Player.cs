using UnityEngine;
using UnityEngine.AI;

public class FollowerAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isFollowing = false;

    public Animator MyAnimator;

    public Collider destinationCollider;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        
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
        // ✅ Solo se l'evento riguarda *questo* oggetto
        if (follower == gameObject)
        {
            Debug.Log($"{name} ha ricevuto l'evento di follow verso {player.name}");
            playerTransform = player.transform;
            isFollowing = true;
            MyAnimator.SetBool("GoToRun", true);
        }
    }


    void Update()
    {
        if (isFollowing && playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
        

    }





}
