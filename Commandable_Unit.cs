using System.Collections;
using UnityEngine;

// Adapter component placed on units to accept centralized orders from OrderManager.
// It uses UnitCommandController for movement if available, or falls back to simple movement.
// It can also issue ForceAttack on RTSUnitAI or call UnitCommandController to approach.
[RequireComponent(typeof(RTSSelectable))]
public class CommandableUnit : MonoBehaviour
{
    private UnitCommandController moveCtrl; // optional movement controller
    private RTSUnitAI aiCtrl;               // optional AI controller
    private UnitWeapon weapon;              // optional weapon component
    private UnitStats stats;                // optional unit stats

    private Coroutine attackRoutine;        // track attack coroutine when doing move-then-attack

    private void Awake()
    {
        // Cache components if present
        moveCtrl = GetComponent<UnitCommandController>();
        aiCtrl = GetComponent<RTSUnitAI>();
        weapon = GetComponent<UnitWeapon>();
        stats = GetComponent<UnitStats>();
    }

    // Called by OrderManager to tell the unit to move to a world position
    public void ReceiveMoveOrder(Vector3 worldPosition)
    {
        // Cancel any ongoing attack routine (we're receiving a new order)
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }

        // If we have a movement controller, use it
        if (moveCtrl != null)
        {
            moveCtrl.SetMoveTarget(worldPosition);
            return;
        }

        // If unit is AI-driven, try to command the AI's agent to move (use ForceAttack with null target as workaround),
        // but better is to integrate a 'SetMoveTarget' on RTSUnitAI; here we try agent if present.
        if (aiCtrl != null)
        {
            // If AI has NavMeshAgent, we can't easily access it from here - use reflection or extend AI with a MoveTo method.
            // For simplicity, we'll attempt to call aiCtrl.ForceAttack(null) as a placeholder — but most setups should use UnitCommandController.
            // Preferred: extend RTSUnitAI with a MoveTo(Vector3) method; we assume that exists in your project.
            // As fallback, teleport a bit (not recommended).
            transform.position = Vector3.Lerp(transform.position, worldPosition, 0.4f);
            return;
        }

        // Ultimate fallback: direct position assignment (teleport-ish)
        transform.position = worldPosition;
    }

    // Called by OrderManager to move to an approach position and then attack the specified target
    public void ReceiveAttackOrder(GameObject target, Vector3 approachPosition)
    {
        // Cancel previous routine
        if (attackRoutine != null) StopCoroutine(attackRoutine);

        // Start coroutine that moves to approach point, then triggers attack behavior
        attackRoutine = StartCoroutine(MoveThenAttackRoutine(target, approachPosition));
    }

    // Coroutine: move to approach point, then attack target using AI or weapon
    private IEnumerator MoveThenAttackRoutine(GameObject target, Vector3 approach)
    {
        // Move to approach position
        if (moveCtrl != null)
        {
            moveCtrl.SetMoveTarget(approach);
            // Wait until close to approach position
            while (Vector3.Distance(transform.position, approach) > 0.75f)
            {
                yield return null;
            }
        }
        else if (aiCtrl != null)
        {
            // If AI exists, try to force chase then attack
            aiCtrl.ForceAttack(target.transform);
            // Wait until AI reaches attack range or target is null / dead
            while (target != null && Vector3.Distance(transform.position, target.transform.position) > (weapon != null ? weapon.attackRange : 2f))
            {
                yield return null;
            }
        }
        else
        {
            // Without movement components, move instantly
            transform.position = approach;
            yield return null;
        }

        // Once in position, perform attack action:
        if (aiCtrl != null)
        {
            // Tell AI to focus on the target
            aiCtrl.ForceAttack(target.transform);
        }
        else if (weapon != null)
        {
            // Use weapon directly (attempt to attack until target dead)
            while (target != null)
            {
                if (weapon.CanAttack())
                    weapon.TryAttack(target);

                // If target is damageable check for death
                var tStats = target.GetComponent<UnitStats>();
                if (tStats != null && tStats.currentHealth <= 0f) break;

                yield return null;
            }
        }

        // Cleanup
        attackRoutine = null;
    }
}
