using System.Collections.Generic;
using UnityEngine;

public class RTSSelectionManager : MonoBehaviour
{
    public Rect selectionRect;                         // Stores the selection rectangle area
    private Vector2 startPos;                          // Mouse position when dragging starts
    private Vector2 endPos;                            // Mouse position when dragging ends
    public List<RTSSelectable> selectedUnits = new();  // Currently selected units

    public LayerMask selectableMask;                   // Filter so only units can be clicked

    public SelectionBoxUI selectionUI;                 // UI rectangle renderer reference

    void Update()
    {
        HandleSelectionBox(); // Main selection logic
    }

    void HandleSelectionBox()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse pressed
        {
            startPos = Input.mousePosition;   // Record starting position
        }

        if (Input.GetMouseButton(0)) // While dragging
        {
            endPos = Input.mousePosition;     // Update ongoing drag position
            selectionRect = CreateRect(startPos, endPos); // Build rect data
            selectionUI.UpdateRectangle(selectionRect);   // Update UI
        }

        if (Input.GetMouseButtonUp(0)) // On release
        {
            SelectUnits();             // Perform the actual selection
            selectionUI.Hide();        // Hide the UI rectangle
        }
    }

    Rect CreateRect(Vector2 p1, Vector2 p2)
    {
        // Create a rect from two opposite corners
        return new Rect(
            Mathf.Min(p1.x, p2.x),
            Mathf.Min(p1.y, p2.y),
            Mathf.Abs(p2.x - p1.x),
            Mathf.Abs(p2.y - p1.y)
        );
    }

    void SelectUnits()
    {
        foreach (RTSSelectable unit in selectedUnits) // Deselect all old units
            unit.Deselect();

        selectedUnits.Clear();                       // Reset list

        RTSSelectable[] allUnits = FindObjectsOfType<RTSSelectable>(); // Get all selectable units

        foreach (var unit in allUnits)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position); // Convert world → screen

            if (selectionRect.Contains(screenPos)) // Check if inside rectangle
            {
                unit.Select();                    // Select this unit
                selectedUnits.Add(unit);          // Add to list
            }
        }
    }
}
