using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public LayerMask groundLayer; // Layer mask for ground detection
    public LayerMask unitLayer;   // Layer mask for unit selection
    private List<Unit> selectedUnits = new List<Unit>();

    void Update()
    {
        HandleSelection();
        HandleMovement();
    }

    void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayer))
            {
                Unit unit = hit.collider.GetComponent<Unit>();
                if (unit != null)
                {
                    if (!Input.GetKey(KeyCode.LeftShift)) // Single selection unless Shift is held
                    {
                        DeselectAllUnits();
                    }
                    SelectUnit(unit);
                }
            }
            else
            {
                DeselectAllUnits();
            }
        }
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0) // Right mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                foreach (Unit unit in selectedUnits)
                {
                    unit.MoveTo(hit.point);
                }
            }
        }
    }

    void SelectUnit(Unit unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.Select();
        }
    }

    void DeselectAllUnits()
    {
        foreach (Unit unit in selectedUnits)
        {
            unit.Deselect();
        }
        selectedUnits.Clear();
    }
}

public class Unit : MonoBehaviour
{
    private bool isSelected = false;
    private Renderer unitRenderer;
    public Color selectedColor = Color.green;
    private Color originalColor;

    private UnityEngine.AI.NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        unitRenderer = GetComponent<Renderer>();
        originalColor = unitRenderer.material.color;
    }

    public void MoveTo(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public void Select()
    {
        isSelected = true;
        unitRenderer.material.color = selectedColor;
    }

    public void Deselect()
    {
        isSelected = false;
        unitRenderer.material.color = originalColor;
    }
}
