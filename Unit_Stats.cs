using UnityEngine; 

// Component that stores unit health and basic stats, and handles death.
// Attach to any unit GameObject that can be damaged.
[RequireComponent(typeof(Collider))]  // Ensure unit has a collider (used by some optional systems)
public class UnitStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;       // Max health value
    public float currentHealth = 100f;   // Current health, change at runtime

    [Header("Combat")]
    public float armor = 0f;             // Simple damage reduction value (flat)
    public float attackThreat = 1f;      // How "threatening" this unit is for AI aggro calculations

    [Header("Death")]
    public GameObject deathPrefab;       // Optional prefab to spawn on death (fx, ragdoll)
    public bool destroyOnDeath = true;   // Whether to Destroy the GameObject when dead

    // Called at script load; initialize current health
    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // Ensure valid health range
    }

    // Applies damage to this unit. Returns true if the unit died.
    // 'source' is optional reference to who caused the damage (used for aggro)
    public bool ApplyDamage(float amount, GameObject source)
    {
        // Reduce damage by armor (simple flat reduction; clamp to minimum zero)
        float effective = Mathf.Max(0f, amount - armor);

        // Subtract from current health
        currentHealth -= effective;

        // If health falls to 0 or below, handle death
        if (currentHealth <= 0f)
        {
            Die(source);
            return true; // died
        }

        // Optionally: react to hit (play hit anim/sound), notify AI etc.
        return false; // still alive
    }

    // Handles unit death logic
    private void Die(GameObject killer)
    {
        // Optional: spawn death effect prefab at unit position
        if (deathPrefab != null)
            Instantiate(deathPrefab, transform.position, transform.rotation);

        // Optional: notify other systems (Event, CombatManager, Score)
        // Example: CombatManager.Instance?.OnUnitKilled(this, killer);

        // Destroy this GameObject if configured
        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false); // otherwise just deactivate
    }
}
