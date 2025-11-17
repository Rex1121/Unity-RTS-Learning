using System.Collections.Generic;                 // For List<T>
using UnityEngine;
using UnityEngine.UI;                              // For Image

// Manages the lifecycle and positioning of unit icons on the mini-map UI.
public class MiniMapIconManager : MonoBehaviour
{
    public static MiniMapIconManager Instance;      // Singleton for easy access

    [Header("Minimap UI")]
    public RectTransform minimapRect;               // RectTransform of the minimap area in the UI
    public Camera minimapCamera;                    // Top-down camera rendering the minimap
    public Canvas minimapCanvas;                    // Canvas that contains minimap icons (Screen Space - Camera or Overlay)

    [Header("Icon Prefab")]
    public GameObject iconPrefab;                   // Simple UI element (Imag) prefab for icons

    // Internal map from unit -> icon instance
    private readonly Dictionary<Transform, RectTransform> _icons = new();

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by a unit when it spawns and needs a minimap icon
    public RectTransform CreateIconForUnit(Transform unit, Sprite iconSprite = null)
    {
        // Instantiate the UI prefab as a child of minimapCanvas
        GameObject go = Instantiate(iconPrefab, minimapCanvas.transform, false);

        // Optionally set sprite if the prefab uses an Image component
        Image img = go.GetComponent<Image>();
        if (img != null && iconSprite != null) img.sprite = iconSprite;

        RectTransform rt = go.GetComponent<RectTransform>();
        _icons[unit] = rt;
        return rt;
    }

    // Called when a unit is destroyed / removed
    public void RemoveIconForUnit(Transform unit)
    {
        if (_icons.TryGetValue(unit, out RectTransform rt))
        {
            Destroy(rt.gameObject);
            _icons.Remove(unit);
        }
    }

    private void LateUpdate()
    {
        // Update each icon position after world objects moved this frame
        foreach (var kv in _icons)
        {
            Transform unit = kv.Key;
            RectTransform iconRT = kv.Value;

            if (unit == null) { Destroy(iconRT.gameObject); continue; }

            // Convert world position to minimap camera viewport (0..1)
            Vector3 viewportPos = minimapCamera.WorldToViewportPoint(unit.position);

            // If unit is behind camera or out of view, hide or clamp icon
            bool visible = viewportPos.z > 0f; // in front of camera
            iconRT.gameObject.SetActive(visible);

            // Map viewport to minimap rect coordinates
            Vector2 local;
            local.x = (viewportPos.x - 0.5f) * minimapRect.rect.width;
            local.y = (viewportPos.y - 0.5f) * minimapRect.rect.height;

            iconRT.anchoredPosition = local;
        }
    }
}
