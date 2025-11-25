using UnityEngine; // Unity core

// Very small projectile script that steers toward a target and deals damage on impact.
// Useful for ranged UnitWeapon projectiles.
[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float lifeTime = 8f;           // Max lifetime before auto-destroy
    public float turnSpeed = 720f;        // Degrees per second to rotate toward target (homing feel)

    private GameObject target;            // Target GameObject to hit
    private float damage = 10f;           // Damage to apply on hit
    private float speed = 10f;            // Movement speed
    private GameObject owner;             // Who fired this projectile (to avoid friendly fire optionally)

    private Rigidbody rb;                 // Rigidbody for physics-based movement
    private float spawnTime;              // Time of spawn

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();   // Cache rigidbody
    }

    // Initialize projectile with parameters (called by UnitWeapon)
    public void Initialize(GameObject target, float damage, float speed, GameObject owner)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.owner = owner;

        spawnTime = Time.time;
    }

    private void Update()
    {
        // Self-destruct after lifeTime
        if (Time.time - spawnTime >= lifeTime) Destroy(gameObject);

        if (target == null)
        {
            // Move forward if no target
            rb.velocity = transform.forward * speed;
            return;
        }

        // Compute desired direction to target (simple homing)
        Vector3 toTarget = (target.transform.position - transform.position).normalized;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            // Very close; skip steering
            rb.velocity = transform.forward * speed;
            return;
        }

        // Rotate smoothly toward target
        Quaternion desired = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);

        // Move forward
        rb.velocity = transform.forward * speed;
    }

    // Handle collision with target or other objects
    private void OnTriggerEnter(Collider other)
    {
        // If hit its intended target (or optionally any damageable), apply damage
        if (other.gameObject == target || other.GetComponent<IDamageable>() != null)
        {
            IDamageable d = other.GetComponent<IDamageable>();
            if (d != null) d.ApplyDamage(damage, owner);
            else
            {
                UnitStats us = other.GetComponent<UnitStats>();
                if (us != null) us.ApplyDamage(damage, owner);
            }

            // Destroy projectile after hit
            Destroy(gameObject);
        }
        else
        {
            // Optionally: destroy on any collision (e.g., environment)
            // Uncomment next line to destroy on first collision:
            // Destroy(gameObject);
        }
    }
}
