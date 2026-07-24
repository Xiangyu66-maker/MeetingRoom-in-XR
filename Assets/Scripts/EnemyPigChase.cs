using UnityEngine;
using UnityEngine.AI;

public class EnemyPigChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 0.2f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float patrolRadius = 8f;
    [SerializeField] private float patrolWaitTime = 2f;

    private NavMeshAgent agent;
    private float patrolTimer = 0f;

    private bool wasHiddenLastFrame = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("EnemyPigChase: No NavMeshAgent found.");
            return;
        }

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("EnemyPigChase: Cannot find object with tag Player.");
            }
        }
    }

    private void Update()
    {
        if (agent == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("EnemyPigChase: Pig is not on NavMesh.");
            return;
        }

        if (PlayerHideState.IsHidden)
        {
            if (!wasHiddenLastFrame)
            {
                OnPlayerStartedHiding();
            }

            PatrolRandomly();
        }
        else
        {
            if (wasHiddenLastFrame)
            {
                OnPlayerStoppedHiding();
            }

            ChasePlayer();
        }

        wasHiddenLastFrame = PlayerHideState.IsHidden;
    }

    private void OnPlayerStartedHiding()
    {
        Debug.Log("Player is hidden. Pig stops chasing and starts patrolling.");

        agent.ResetPath();
        patrolTimer = patrolWaitTime;

        SetRandomPatrolDestination();
    }

    private void OnPlayerStoppedHiding()
    {
        Debug.Log("Player came out. Pig resumes chasing.");

        agent.ResetPath();
        patrolTimer = 0f;
    }

    private void ChasePlayer()
    {
        if (player == null)
        {
            return;
        }

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(player.position);
    }

    private void PatrolRandomly()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                SetRandomPatrolDestination();
                patrolTimer = 0f;
            }
        }
    }

    private void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("Pig is patrolling randomly.");
        }
    }
}