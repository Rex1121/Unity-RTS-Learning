using UnityEngine;
using UnityEngine.UI; 

// Manages a simple nameplate + health bar for the unit.
// Expects a prefab that is a small world-space canva with:
// - Image healthFill (anchor left-to-right) and Text nameText
public class UnitUI_Nameplate : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject nameplatePrefab;      // Prefab with Canvas (world-space) and visuals
    public Vector3 offset = new Vector3(0, 2f, 0); // Offset above unit

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    private Transform nameplateInstance;    // Instance of the nameplate
    private Image healthFillImage;          // Fill imag to update
    private Text nameText;                  // Name text (use TMP if desired)

    private void Start()
    {
        // Instantiate nameplate and parent to world rot (so it is not tied to unit transform scaling)
        GameObject go = Instantiate(nameplatePrefab);
        nameplateInstance = go.transform;

        // Try to find common components
        healthFillImage = go.GetComponentInChildren<Image>();
        nameText = go.GetComponentInChildren<Text>();

        // Set initial name & health visual
        if (nameText != null) nameText.text = gameObject.name;
        UpdateHealthVisual();
    }

    private void Update()
    {
        if (nameplateInstance == null) return;

        // Position the nameplate above the unit in world space
        nameplateInstance.position = transform.position + offset;

        // Face the camera (billboard)
        Camera cam = Camera.main;
        if (cam != null) nameplateInstance.rotation = Quaternion.LookRotation(nameplateInstance.position - cam.transform.position);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        UpdateHealthVisual();

        if (currentHealth <= 0f) Die();
    }

    private void UpdateHealthVisual()
    {
        if (healthFillImage != null)
        {
            float pct = Mathf.Clamp01(currentHealth / maxHealth);
            // Assumes fill type is horizontal left-to-right
            healthFillImage.fillAmount = pct;
        }
    }

    private void Die()
    {
        // Optional: destroy unit and UI
        if (nameplateInstance != null) Destroy(nameplateInstance.gameObject);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Ensure UI removed if unit destroyed by other means
        if (nameplateInstance != null) Destroy(nameplateInstance.gameObject);
    }
}
