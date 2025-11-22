// Simple interface defining an object that can take damage.
// Implement this on any object that should respond to damage (units, buildings, etc.).
public interface IDamageable
{
    // Apply damage to the object. Returns true if object died as a result of the damage.
    bool ApplyDamage(float amount, GameObject source);
}
