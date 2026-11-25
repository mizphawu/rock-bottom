using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Monster : MonoBehaviour
{
    [Header("Target & Patrol")]
    public Transform player;
    public Transform[] patrolPoints;

    [Header("Detection & Speed")]
    public float detectionRange = 18f;
    public float losePlayerRange = 28f;
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 6.2f;

    [Header("Catch Distance")]
    public float catchRange = 1.8f;

    private NavMeshAgent agent;
    private PlayerStats playerStats;
    private int currentPatrolIndex = 0;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player != null) playerStats = player.GetComponent<PlayerStats>();
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isChasing && dist <= detectionRange)
        {
            isChasing = true;
            if (playerStats != null)
            {
                playerStats.isBeingChased = true;
                playerStats.ShowThought("Something's out there—RUN!");
            }
        }
        else if (isChasing && dist >= losePlayerRange)
        {
            isChasing = false;
            if (playerStats != null) playerStats.isBeingChased = false;
            GoToNextPatrolPoint();
        }

        if (isChasing)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            if (dist <= catchRange && playerStats != null)
            {
                playerStats.OnCaughtByMonster();
            }
        }
        else
        {
            agent.speed = patrolSpeed;
            if (!agent.pathPending && agent.remainingDistance < 0.8f)
            {
                GoToNextPatrolPoint();
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
}