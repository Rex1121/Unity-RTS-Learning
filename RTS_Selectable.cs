using System.Collections.Generic;
using UnityEngine;

public class RTSSelectable : MonoBehaviour
{
    public bool isSelected = false;              // Tracks whether this unit is selected
    public Renderer unitRenderer;                 // Reference to the unit’s renderer (for outline/color change)
    public Color selectedColor = Color.yellow;    // Color when selected
    public Color defaultColor = Color.white;      // Color when not selected

    private void Start()
    {
        unitRenderer = GetComponentInChildren<Renderer>(); // Get renderer from children
        unitRenderer.material.color = defaultColor;        // Set default color
    }

    public void Select()
    {
        isSelected = true;                          // Set flag
        unitRenderer.material.color = selectedColor; // Change color to selected
    }

    public void Deselect()
    {
        isSelected = false;                         // Clear flag
        unitRenderer.material.color = defaultColor; // Restore color
    }
}
