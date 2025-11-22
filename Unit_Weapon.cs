using UnityEngine; // Unity core

// A simple weapon component supporting melee and ranged attacks.
// Attach to the unit and configure for melee (no projectile) or ranged (projectilePrefab).
[RequireComponent(typeof(Animator))] // Optional: animator to play attack animations; still works if missing (we check)
public class UnitWeapon : MonoBehaviour
{
    public enum WeaponType { Melee, Ranged } // Weapon type enum

    [Header("Weapon Settings")]
    public WeaponType weaponType = WeaponType.Melee;  // Is this a melee or ranged weapon?
    public float damage = 20f;                       // Damage per attack
    public float attackRange = 2f;                   // Range in world units to hit a target
    public float attackCooldown = 1.2f;              // Seconds between attacks
    public float attackDelay = 0.2f;                 // Delay between attack trigger and damage application (timing for animation)

    [Header("Ranged Settings")]
    public GameObject projectilePrefab;              // Projectile prefab (for ranged)
    public Transform muzzlePoint;                    // World position to spawn projectile from
    public float projectileSpeed = 18f;              // Speed of spawned projectile

    [Header("Animation")]
    public string attackTriggerName = "Attack";      // Animator trigger name to play attack animation (optional)

    private float lastAttackTime = -999f;            // Time when last attack happened
    private Animator animator;                       // Optional animator reference

    private void Awake()
    {
        // Try to get animator but don't require it (some setups may not have animations)
        animator = GetComponent<Animator>();
    }

    // Returns true if weapon is ready to attack (cooldown expired)
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    // Initiate an attack against a target; returns true if attack started
    // The damage will be applied after attackDelay (useful to sync with animation)
    public bool TryAttack(GameObject target)
    {
        if (target == null) return false;           // No target
        if (!CanAttack()) return false;             // Still cooling down

        // Trigger animation if available
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            animator.SetTrigger(attackTriggerName);

        // Record attack time for cooldown
        lastAttackTime = Time.time;

        // Start delayed attack effect (damage or projectile spawn)
        StartCoroutine(PerformAttackAfterDelay(target, attackDelay));

        return true;
    }

    // Coroutine that waits an attackDelay then applies damage or fires a projectile
    private System.Collections.IEnumerator PerformAttackAfterDelay(GameObject target, float delay)
    {
        // Wait the specified delay (allow animation to play and weapon swing to "reach" target)
        yield return new WaitForSeconds(delay);

        // If target died or was destroyed during delay, abort
        if (target == null) yield break;

        // Melee: apply direct damage if target still in range
        if (weaponType == WeaponType.Melee)
        {
            // Ensure still in range
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange + 0.1f)
            {
                // Try to apply damage via IDamageable
                IDamageable dmg = target.GetComponent<IDamageable>();
                if (dmg != null) dmg.ApplyDamage(damage, gameObject);
                else
                {
                    // If target doesn't implement IDamageable, try UnitStats as fallback
                    UnitStats us = target.GetComponent<UnitStats>();
                    if (us != null) us.ApplyDamage(damage, gameObject);
                }
            }
        }
        else // Ranged weapon: spawn projectile that will handle movement and damage
        {
            // Sanity: need a projectile prefab and muzzle point
            if (projectilePrefab == null || muzzlePoint == null) yield break;

            // Instantiate projectile and configure it
            GameObject projGo = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.identity);
            SimpleProjectile proj = projGo.GetComponent<SimpleProjectile>();
            if (proj != null)
            {
                proj.Initialize(target, damage, projectileSpeed, gameObject); // set target and damage
            }
            else
            {
                // If projectile doesn't have SimpleProjectile component, attempt to give it velocity
                Rigidbody rb = projGo.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (target.transform.position - muzzlePoint.position).normalized;
                    rb.velocity = dir * projectileSpeed;
                }
            }
        }
    }
}
