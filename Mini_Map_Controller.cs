using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    public Camera miniMapCamera;           // Reference to the top-down mini-map camera
    public Transform mainCameraRig;        // Reference to main RTS camera rig
    public float moveSpeed = 100f;         // Speed to move main camera when clicking mini-map

    private bool isDragging = false;       // Track drag state
    private Vector3 dragStartPoint;        // World-space point where drag started

    void Update()
    {
        HandleMiniMapInput(); // All mini-map logic handled here
    }

    void HandleMiniMapInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click down
        {
            if (IsMouseOverMiniMap()) // Ensure click happened ON mini-map
            {
                isDragging = true;         // Begin drag
                dragStartPoint = GetWorldPointFromMiniMap(); // Record world start
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 curPoint = GetWorldPointFromMiniMap(); // Current drag world position
            Vector3 offset = dragStartPoint - curPoint;    // Offset between points

            mainCameraRig.position += offset;              // Move camera accordingly
        }

        if (Input.GetMouseButtonUp(0)) // On release
        {
            if (!isDragging) return;     // Safety
            isDragging = false;          // End drag
        }

        if (Input.GetMouseButtonDown(1) && IsMouseOverMiniMap()) // Right-click mini-map
        {
            Vector3 worldPoint = GetWorldPointFromMiniMap();     // Get click point
            MoveCameraTo(worldPoint);                            // Move to location
        }
    }

    bool IsMouseOverMiniMap()
    {
        // Cast a ray from screen position into UI layer
        Vector3 mousePos = Input.mousePosition;

        // Assumes mini-map camera renders only mini-map layer
        Ray ray = miniMapCamera.ScreenPointToRay(mousePos);

        return Physics.Raycast(ray, out _); // If ray hits anything, we assume it's the minimap plane
    }

    Vector3 GetWorldPointFromMiniMap()
    {
        // Convert mouse position → world position via mini-map camera
        Ray ray = miniMapCamera.ScreenPointToRay(Input.mousePosition);

        // We intersect with a flat horizontal plane at y = 0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        groundPlane.Raycast(ray, out float enter); // Calculate intersection distance

        return ray.GetPoint(enter); // Return world position where ray hits plane
    }

    void MoveCameraTo(Vector3 worldPoint)
    {
        // Keeps current height but moves X & Z
        mainCameraRig.position = Vector3.Lerp(
            mainCameraRig.position,
            new Vector3(worldPoint.x, mainCameraRig.position.y, worldPoint.z),
            Time.deltaTime * moveSpeed
        );
    }
}
