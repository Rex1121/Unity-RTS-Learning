using System.Collections.Generic;
using UnityEngine;

// Provides a simple building placement workflow:
// - Enter placement mode with a prefab
// - Show ghost (transparent) preview that snaps to grid
// - Validate collisions and availability
// - Place building with left click, cancel with right click
public class BuildingPlacementSystem : MonoBehaviour
{
    [Header("Placement")]
    public LayerMask placementBlockedLayers;    // Layers that block placing buildings (e.g., terrain collisions, other buildings)
    public float gridSize = 1f;                 // Size of placement grid snapping
    public Material ghostValidMat;              // Material to indicate valid placement
    public Material ghostInvalidMat;            // Material to indicate invalid placement
    public float maxPlacementDistance = 1000f;  // Max raycast distance

    private GameObject currentGhost;            // Active ghost preview
    private GameObject buildingPrefab;          // The prefab we are placing
    private bool placing = false;               // Are we in placement mode?

    // Start placing a building prefab
    public void StartPlacing(GameObject prefab)
    {
        StopPlacing();                          // Clean previous
        placing = true;
        buildingPrefab = prefab;
        currentGhost = Instantiate(buildingPrefab);
        MakeGhost(currentGhost);
    }

    // Cancel and clean preview
    public void StopPlacing()
    {
        placing = false;
        buildingPrefab = null;
        if (currentGhost != null) Destroy(currentGhost);
    }

    private void Update()
    {
        if (!placing) return;

        // Move ghost to mouse world position on terrain
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance))
        {
            Vector3 snapped = SnapToGrid(hit.point);
            currentGhost.transform.position = snapped;
            currentGhost.transform.rotation = Quaternion.identity; // optionally rotate with keys

            bool valid = IsPlacementValid(currentGhost);

            // Set ghost material to valid/invalid
            SetGhostMaterial(currentGhost, valid ? ghostValidMat : ghostInvalidMat);

            // Left click to confirm placement
            if (Input.GetMouseButtonDown(0) && valid)
            {
                PlaceBuilding(snapped);
            }

            // Right click to cancel
            if (Input.GetMouseButtonDown(1))
            {
                StopPlacing();
            }
        }
    }

    // Snap a world position to grid
    private Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = position.y; // keep height as is (or snap to terrain height)
        float z = Mathf.Round(position.z / gridSize) * gridSize;
        return new Vector3(x, y, z);
    }

    // Simple overlap check to determine if ghost collides with blocking layers
    private bool IsPlacementValid(GameObject ghost)
    {
        // Collect all colliders for ghost (use colliders on prefab)
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            // Use the collider's bounds to test overlap with world
            Collider[] hits = Physics.OverlapBox(c.bounds.center, c.bounds.extents * 0.98f, c.transform.rotation, placementBlockedLayers);
            if (hits.Length > 0) return false;
        }
        return true;
    }

    // Replace ghost with actual building instance (and enable its colliders)
    private void PlaceBuilding(Vector3 position)
    {
        GameObject placed = Instantiate(buildingPrefab, position, Quaternion.identity);
        // Optionally initialize building (owner, health, etc.)

        // End placement (or keep placing multiples by not stopping)
        StopPlacing();
    }

    // Set render material for the ghost (walk all renderers)
    private void SetGhostMaterial(GameObject ghost, Material mat)
    {
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.sharedMaterial = mat;
        }
    }

    // Convert the prefab into a ghost (disable colliders and make semi-transparent)
    private void MakeGhost(GameObject ghost)
    {
        // Remove/disable all colliders so physics checks use explicit OverlapBox instead
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        // Optionally set a translucent material; relying on SetGhostMaterial calls.
    }
}
