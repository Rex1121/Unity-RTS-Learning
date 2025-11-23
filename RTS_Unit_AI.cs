using UnityEngine;
using System.Collections.Generic;
#if UNITY_2020_1_OR_NEWER
using UnityEngine.AI; // NavMeshAgent for pathfinding (optional)
#endif

// Core AI state machine for RTS units. Supports patrol, idle, alert (chase), and attack states.
// Attach this to unit prefabs with UnitStats and UnitWeapon components.
[RequireComponent(typeof(UnitStats))]
[RequireComponent(typeof(UnitWeapon))]
public class RTSUnitAI : MonoBehaviour
{
    // AI States
    private enum State { Idle, Patrol, Alert, Chase, Attack }

    [Header("AI Settings")]
    public float sightRange = 15f;                 // How far unit can "see" enemies
    public float loseInterestRange = 25f;          // If enemy goes beyond this, unit will stop chasing
    public float attackRangeBuffer = 0.2f;         // Small buffer for attack range checks
    public float aggroCooldown = 5f;               // Time to remain aggro after losing sight

    [Header("Patrol")]
    public Transform[] patrolPoints;               // Optional patrol points (set in Inspector)
    public float patrolSpeed = 3.5f;               // Movement speed while patrolling

    [Header("Movement")]
    public float moveSpeed = 5f;                   // Movement speed if no NavMeshAgent
    public float rotationSpeed = 720f;             // Rotation speed for non-nav movement (deg/sec)

    [Header("Team")]
    public int teamId = 0;                         // Team/faction id (used to avoid attacking friends)

    [Header("Animation (optional)")]
    public Animator animator;                      // Optional animator to play movement/attack animations
    public string moveParam = "MoveSpeed";         // Animator float parameter to indicate movement speed

    // Internal references
    private UnitStats stats;                       // Reference to unit stats
    private UnitWeapon weapon;                     // Reference to weapon
    private State state = State.Idle;              // Current AI state
    private int patrolIndex = 0;                   // Current index into patrolPoints
    private Transform currentTarget = null;        // Current enemy target
    private float lastSeenTime = -999f;            // Last time we saw a target (for aggro cooldown)
    private float lastAggroTime = -999f;           // Last time we were aggroed

    // NavMeshAgent (optional). If present, AI uses it for navigation.
#if UNITY_2020_1_OR_NEWER
    private NavMeshAgent agent;
#endif

    private void Awake()
    {
        // Cache components
        stats = GetComponent<UnitStats>();
        weapon = GetComponent<UnitWeapon>();

        // Try to get NavMeshAgent (may not be present — code handles fallback)
#if UNITY_2020_1_OR_NEWER
        agent = GetComponent<NavMeshAgent>();
#endif

        // If animator not assigned, try to fetch one on same GameObject
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Initialize patrol if available
        if (patrolPoints != null && patrolPoints.Length > 0)
            state = State.Patrol;
        else
            state = State.Idle;
    }

    private void Update()
    {
        // Update animator movement parameter if available (simple speed magnitude)
        if (animator != null)
        {
#if UNITY_2020_1_OR_NEWER
            float spd = (agent != null) ? agent.velocity.magnitude : 0f;
#else
            float spd = 0f;
#endif
            animator.SetFloat(moveParam, spd);
        }

        // Each frame, do perception then act based on current state
        PerceptionUpdate();
        StateUpdate();
    }

    // PERCEPTION: find nearest hostile unit within sightRange
    private void PerceptionUpdate()
    {
        // Build a list of candidate targets by sphere overlap
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange);
        Transform nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            // Skip self
            if (c.transform == transform) continue;

            // Only consider objects with UnitStats (i.e., valid units/buildings)
            UnitStats other = c.GetComponent<UnitStats>();
            if (other == null) continue;

            // Team check: skip friendly units
            RTSUnitAI otherAI = other.GetComponent<RTSUnitAI>();
            int otherTeam = (otherAI != null) ? otherAI.teamId : 0;
            if (otherTeam == teamId) continue; // same team

            // Compute distance and pick nearest
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = other.transform;
            }
        }

        // If we found an enemy, set as current target and record time seen
        if (nearest != null)
        {
            currentTarget = nearest;
            lastSeenTime = Time.time;
            lastAggroTime = Time.time;
        }
        else
        {
            // If no enemy in perception this frame, we leave currentTarget as-is but rely on aggro timeout/lose conditions in StateUpdate
        }
    }

    // STATE MACHINE logic
    private void StateUpdate()
    {
        switch (state)
        {
            case State.Idle:
                DoIdle();
                break;
            case State.Patrol:
                DoPatrol();
                break;
            case State.Alert:
                DoAlert();
                break;
            case State.Chase:
                DoChase();
                break;
            case State.Attack:
                DoAttack();
                break;
        }

        // Global transition: if we have a current target and are not already chasing/attacking, become Alert/Chase
        if (currentTarget != null && state != State.Chase && state != State.Attack)
        {
            // Transition to chase immediately (alert could be used for animations)
            state = State.Chase;
        }
    }

    // Idle behaviour — do nothing, maybe wander
    private void DoIdle()
    {
        // Optional: small idle behavior like look around or play idle anim
        // If patrol points exist, switch to patrol
        if (patrolPoints != null && patrolPoints.Length > 0)
            state = State.Patrol;
    }

    // Patrol behaviour — move between patrol points in order
    private void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            state = State.Idle;
            return;
        }

        Transform targetPoint = patrolPoints[patrolIndex];
        if (targetPoint == null) { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; return; }

        // Move toward patrol point
        MoveTo(targetPoint.position, patrolSpeed);

        // If close enough, advance to next point
        if (Vector3.Distance(transform.position, targetPoint.position) <= 1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
    }

    // Alert behaviour — short-lived state before chase (can play animation)
    private void DoAlert()
    {
        // Could play an alert animation here, then transition to Chase
        state = State.Chase;
    }

    // Chase behaviour — pursue target
    private void DoChase()
    {
        if (currentTarget == null)
        {
            // Lost target — revert to idle or patrol after aggro timeout
            if (Time.time - lastAggroTime > aggroCooldown)
            {
                currentTarget = null;
                state = (patrolPoints != null && patrolPoints.Length > 0) ? State.Patrol : State.Idle;
            }
            return;
        }

        // If target very far beyond loseInterestRange, drop target
        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distToTarget > loseInterestRange)
        {
            currentTarget = null;
            state = (patrolPoints != null && patrolPoints.Length > 0) ? State.Patrol : State.Idle;
            return;
        }

        // If target within attack range, switch to Attack state
        if (weapon != null && distToTarget <= weapon.attackRange + attackRangeBuffer)
        {
            state = State.Attack;
            return;
        }

        // Otherwise, continue moving toward target
        MoveTo(currentTarget.position, moveSpeed);
    }

    // Attack behaviour — attempt to use weapon on target
    private void DoAttack()
    {
        if (currentTarget == null)
        {
            // Target lost — go back to chase/idle
            state = State.Chase;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // If target moved out of weapon range, go back to chase
        if (dist > weapon.attackRange + attackRangeBuffer)
        {
            state = State.Chase;
            return;
        }

        // Face target (for melee/animation aesthetics)
        FaceTarget(currentTarget.position);

        // Try attacking if weapon ready
        if (weapon.CanAttack())
        {
            weapon.TryAttack(currentTarget.gameObject);

            // Optionally: if weapon is ranged, add slight repositioning to dodge or strafe
        }
    }

    // Helper to move to a position using NavMeshAgent if present, otherwise direct translate
    private void MoveTo(Vector3 destination, float speed)
    {
#if UNITY_2020_1_OR_NEWER
        if (agent != null)
        {
            // If agent present, set destination (agent handles pathfinding)
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(destination);
            return;
        }
#endif
        // Fallback: simple movement toward destination (non-navmesh)
        Vector3 dir = (destination - transform.position);
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.01f) return;

        // Rotate
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // Move forward clamped by speed
        Vector3 move = transform.forward * speed * Time.deltaTime;
        if (move.magnitude > dist) move = dir;
        transform.position += move;
    }

    // Smoothly face a position
    private void FaceTarget(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    // Public method to force this unit to attack a specific target (external orders/aggro)
    public void ForceAttack(Transform target)
    {
        currentTarget = target;
        lastSeenTime = Time.time;
        lastAggroTime = Time.time;
        state = State.Chase;
    }

    // Optional: visualize sight/attack ranges in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        if (weapon != null) Gizmos.DrawWireSphere(transform.position, weapon.attackRange);
    }
}
