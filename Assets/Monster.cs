using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Monster : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public PlayerStats playerStats;

    [Header("Territory Box Collider (Start & End Zone)")]
    public Collider territoryZone;
    public bool hideUntilPlayerEnters = false;

    [Header("Detection & Chase")]
    public float detectionRange = 120f;
    public float loseSightRange = 130f;
    public float attackRange = 2.0f;
    public float patrolSpeed = 2.2f;
    public float chaseSpeed = 5.2f;

    [Header("Fallback Radius (If No Territory Box Assigned)")]
    public float maxTerritoryRadius = 35f;
    public float patrolRadius = 15f;
    public float waitTimeAtPoint = 2.5f;

    [Header("Combat & Catch")]
    public float attackCooldown = 1.8f;

    private NavMeshAgent agent;
    private Animator animator;
    private Renderer[] renderers;
    private Vector3 spawnPosition;
    private float waitTimer;
    private float attackTimer;
    private bool isChasing;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<Renderer>();
        spawnPosition = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && playerStats == null)
        {
            playerStats = player.GetComponentInChildren<PlayerStats>();
        }

        if (hideUntilPlayerEnters)
            SetVisible(false);

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        attackTimer -= Time.deltaTime;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is inside the Start/End Box Collider
        bool playerInTerritory = territoryZone != null
            ? territoryZone.bounds.Contains(player.position)
            : Vector3.Distance(spawnPosition, player.position) <= maxTerritoryRadius;

        // 1. Start / End Chase based on Territory Box Collider
        if (!isChasing && playerInTerritory && distanceToPlayer <= detectionRange)
        {
            isChasing = true;
            if (hideUntilPlayerEnters) SetVisible(true);
        }
        else if (isChasing && (!playerInTerritory || distanceToPlayer > loseSightRange))
        {
            isChasing = false;
            if (hideUntilPlayerEnters) SetVisible(false);
            PickNewPatrolPoint();
        }

        // Sync chase state with PlayerStats so anxiety rises while being chased
        if (playerStats != null)
        {
            playerStats.isBeingChased = isChasing;
        }

        // 2. Execute Behavior
        if (isChasing)
        {
            ChaseAndAttack(distanceToPlayer);
        }
        else
        {
            Patrol();
        }

        // 3. Update Animator Speed
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                waitTimer = 0f;
                PickNewPatrolPoint();
            }
        }
    }

    void ChaseAndAttack(float distance)
    {
        if (distance <= attackRange)
        {
            agent.isStopped = true;
            FacePlayer();

            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;

                if (animator != null)
                    animator.SetTrigger("Attack");

                if (playerStats != null)
                    playerStats.OnCaughtByMonster();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    void PickNewPatrolPoint()
    {
        Vector3 targetPoint;

        if (territoryZone != null)
        {
            // Pick a random point strictly inside the Box Collider's bounds
            Bounds b = territoryZone.bounds;
            targetPoint = new Vector3(
                Random.Range(b.min.x, b.max.x),
                transform.position.y,
                Random.Range(b.min.z, b.max.z)
            );
        }
        else
        {
            targetPoint = Random.insideUnitSphere * patrolRadius + spawnPosition;
        }

        if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime
            );
        }
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (territoryZone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(territoryZone.bounds.center, territoryZone.bounds.size);
        }
    }
}