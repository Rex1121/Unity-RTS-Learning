using UnityEngine;
using UnityEngine.AI;

// Core RTS Unit AI with explicit command API.
// Supports player-issued Move / Stop / Attack orders AND autonomous AI behavior.
[RequireComponent(typeof(UnitStats))]
[RequireComponent(typeof(UnitWeapon))]
public class RTSUnitAI : MonoBehaviour
{
    // All high-level behavior states
    private enum State
    {
        Idle,       // Standing still / waiting
        Patrol,    // Following patrol points
        MoveOrder, // Player-issued move command
        Chase,     // Chasing a target
        Attack     // Actively attacking
    }

    [Header("AI Settings")]
    public float sightRange = 15f;           // Enemy detection radius
    public float loseInterestRange = 25f;    // Distance where chase stops
    public float attackBuffer = 0.2f;        // Extra range tolerance for attacks
    public float aggroCooldown = 5f;          // Time before AI gives up after losing target

    [Header("Patrol")]
    public Transform[] patrolPoints;         // Optional patrol route

    [Header("Movement (Non-NavMesh fallback)")]
    public float moveSpeed = 5f;              // Speed if NavMeshAgent missing
    public float rotationSpeed = 720f;        // Turning speed (degrees/sec)

    [Header("Team")]
    public int teamId = 0;                    // Used to avoid friendly fire

    // Cached components
    private UnitStats stats;
    private UnitWeapon weapon;
    private NavMeshAgent agent;

    // Internal state
    private State state = State.Idle;         // Current behavior state
    private Transform currentTarget;          // Enemy being attacked
    private Vector3 moveDestination;          // Destination for MoveTo orders
    private int patrolIndex = 0;              // Current patrol point index
    private float lastSeenTime;               // Last time target was visible

    // -------------------- UNITY LIFECYCLE --------------------

    private void Awake()
    {
        // Cache required components
        stats = GetComponent<UnitStats>();
        weapon = GetComponent<UnitWeapon>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Start patrolling if patrol points exist
        if (patrolPoints != null && patrolPoints.Length > 0)
            state = State.Patrol;
        else
            state = State.Idle;
    }

    private void Update()
    {
        // Perception runs regardless of state (unless player overrides)
        if (state != State.MoveOrder)
            ScanForEnemies();

        // Execute behavior for current state
        switch (state)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Patrol:UpdatePatrol();break;
            case State.MoveOrder: UpdateMoveOrder(); break;
            case State.Chase:  UpdateChase(); break;
            case State.Attack: UpdateAttack();break;
        }
    }

    // -------------------- PUBLIC COMMAND API --------------------

    /// <summary>
    /// Player-issued move command.
    /// Cancels AI behaviors and moves unit to destination.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        // Clear combat target
        currentTarget = null;

        // Store destination
        moveDestination = destination;

        // Switch to player-controlled movement
        state = State.MoveOrder;

        // Use NavMesh if available
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// Immediately stops all movement and AI actions.
    /// </summary>
    public void Stop()
    {
        // Stop NavMesh movement if used
        if (agent != null)
            agent.isStopped = true;

        // Clear target and return to idle
        currentTarget = null;
        state = State.Idle;
    }

    /// <summary>
    /// Player-issued attack command.
    /// Unit will chase and attack target until dead or out of range.
    /// </summary>
    public void AttackTarget(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        lastSeenTime = Time.time;
        state = State.Chase;
    }

    // -------------------- PERCEPTION --------------------

    // Detect nearby enemy units
    private void ScanForEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange);

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;

            UnitStats otherStats = hit.GetComponent<UnitStats>();
            RTSUnitAI otherAI = hit.GetComponent<RTSUnitAI>();

            // Ignore non-units or friendly units
            if (otherStats == null || otherAI == null) continue;
            if (otherAI.teamId == teamId) continue;

            // Acquire target
            currentTarget = hit.transform;
            lastSeenTime = Time.time;
            state = State.Chase;
            return;
        }
    }

    // -------------------- STATE UPDATES --------------------

    private void UpdateIdle()
    {
        // Do nothing — waiting for orders or enemies
    }

    private void UpdatePatrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform point = patrolPoints[patrolIndex];

        MoveInternal(point.position);

        // Advance patrol when close
        if (Vector3.Distance(transform.position, point.position) < 1f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateMoveOrder()
    {
        // Check arrival
        float dist = Vector3.Distance(transform.position, moveDestination);
        if (dist <= 0.5f)
        {
            Stop();
        }
        else
        {
            MoveInternal(moveDestination);
        }
    }

    private void UpdateChase()
    {
        if (currentTarget == null)
        {
            state = State.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // Drop aggro if target too far
        if (dist > loseInterestRange)
        {
            currentTarget = null;
            state = State.Idle;
            return;
        }

        // Enter attack range
        if (dist <= weapon.attackRange + attackBuffer)
        {
            state = State.Attack;
            return;
        }

        // Move closer
        MoveInternal(currentTarget.position);
    }

    private void UpdateAttack()
    {
        if (currentTarget == null)
        {
            state = State.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // Target escaped attack range
        if (dist > weapon.attackRange + attackBuffer)
        {
            state = State.Chase;
            return;
        }

        FaceTarget(currentTarget.position);

        // Attempt attack
        if (weapon.CanAttack())
            weapon.TryAttack(currentTarget.gameObject);
    }

    // -------------------- MOVEMENT HELPERS --------------------

    // Internal movement that works with or without NavMeshAgent
    private void MoveInternal(Vector3 destination)
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
            return;
        }

        // Non-NavMesh fallback
        Vector3 dir = destination - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rot,
            rotationSpeed * Time.deltaTime
        );

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    // Rotate toward a world position
    private void FaceTarget(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rot,
            rotationSpeed * Time.deltaTime
        );
    }

    // -------------------- DEBUG --------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (weapon != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, weapon.attackRange);
        }
    }
}
