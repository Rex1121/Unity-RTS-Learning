using System.Collections.Generic;
using UnityEngine;

public class RTSCameraController_Advanced : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;          // Base movement speed
    public float fastMoveMultiplier = 2f;  // Speed multiplier when holding Shift
    public float edgeScrollSize = 20f;     // Edge thicknes for edge scrolling

    [Header("Smoothing")]
    public float movementSmoothTime = 0.12f; // Time for smooth damping movement
    public float rotationSmoothTime = 0.1f;  // Time for smooth damping rotations
    public float zoomSmoothTime = 0.1f;      // Time for smoth damping zoom

    [Header("Zoom")]
    public float zoomSpeed = 200f;          // Scroll wheel zoom speed
    public float minZoom = 10f;             // Minimum FOV
    public float maxZoom = 80f;             // Maximum FOV

    [Header("Rotation")]
    public float rotateSpeed = 120f;        // Rotation drag speed

    [Header("Drag Panning")]
    public float dragPanSpeed = 1.2f;       // Speed of middle-mous drag panning

    [Header("Bounds (Optional)")]
    public bool useBounds = false;          // Toggle bounding
    public Vector2 minBounds;               // Min X/Z
    public Vector2 maxBounds;               // Max X/Z

    private Camera cam;                     // Cache reference to camera
    private Vector3 velocity = Vector3.zero;       // SmoothDamp velocity for movement
    private float fovVelocity = 0f;                 // SmoothDamp velocity for zoom
    private float rotationVelocity = 0f;            // SmoothDamp velocity  rotation
    private float targetRotationY;                  // Smoothed Y rotation target

    private void Start()
    {
        cam = Camera.main;                        // Get main camera reference
        targetRotationY = transform.eulerAngles.y; // Initialize rotation target
    }

    private void Update()
    {
        HandleMovement();   // Smooth WASD + edges + drag pan
        HandleRotation();   // Smooth rotation
        HandleZoom();       // Smooth zoom
    }

    void HandleMovement()
    {
        Vector3 targetMove = Vector3.zero;  // Desired movement before smoothing

        bool fast = Input.GetKey(KeyCode.LeftShift); // Check for fast speed modifer
        float speed = fast ? moveSpeed * fastMoveMultiplier : moveSpeed;

        // WASD MOVEMENT
        if (Input.GetKey(KeyCode.W)) targetMove += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) targetMove += Vector3.back;
        if (Input.GetKey(KeyCode.A)) targetMove += Vector3.left;
        if (Input.GetKey(KeyCode.D)) targetMove += Vector3.right;

        // EDGE SCROLL MOVEMENT
        Vector3 mouse = Input.mousePosition;
        if (mouse.x <= edgeScrollSize) targetMove += Vector3.left;
        if (mouse.x >= Screen.width - edgeScrollSize) targetMove += Vector3.right;
        if (mouse.y <= edgeScrollSize) targetMove += Vector3.back;
        if (mouse.y >= Screen.height - edgeScrollSize) targetMove += Vector3.forward;

        // DRAG PANNING (Middle Mouse Button)
        if (Input.GetMouseButton(2))
        {
            float dragX = -Input.GetAxis("Mouse X") * dragPanSpeed;
            float dragZ = -Input.GetAxis("Mouse Y") * dragPanSpeed;

            targetMove += new Vector3(dragX, 0, dragZ);
        }

        // ROTATE MOVEMENT VECTOR TO MATCH CAMERA ORIENTATION
        targetMove = Quaternion.Euler(0, transform.eulerAngles.y, 0) * targetMove;

        // APPLY SPEED AND TIME
        targetMove *= speed;

        // SMOOTH FINAL MOVEMENT
        transform.position = Vector3.SmoothDamp(
            transform.position,                    // Current
            transform.position + targetMove,       // Target
            ref velocity,                          // Velocity ref
            movementSmoothTime                     // Smooth time
        );

        // APPLY MAP BOUNDS
        if (useBounds)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x),
                transform.position.y,
                Mathf.Clamp(transform.position.z, minBounds.y, maxBounds.y)
            );
        }
    }

    void HandleRotation()
    {
        // RIGHT MOUSE DRAG = ROTATE CAMERA
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");         // Read horizontal drag
            targetRotationY += mouseX * rotateSpeed * Time.deltaTime; // Adjust rotation
        }

        // SMOOTH ROTATION USING DAMPING
        float newY = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,      // Current angl
            targetRotationY,              // Target angle
            ref rotationVelocity,         // Velocity ref
            rotationSmoothTime            // Smooth time
        );

        transform.rotation = Quaternion.Euler(0, newY, 0); // Apply rotation
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Read scroll input

        if (scroll != 0)
        {
            float targetFov = cam.fieldOfView - scroll * zoomSpeed * Time.deltaTime; // Calculate desired FOV
            targetFov = Mathf.Clamp(targetFov, minZoom, maxZoom);                    // Clamp to limts

            cam.fieldOfView = Mathf.SmoothDamp(
                cam.fieldOfView,               // Current FOV
                targetFov,                    // Target FOV
                ref fovVelocity,              // Smooth velocity
                zoomSmoothTime                // Smooth time
            );
        }
    }
}
