using System.Collections.Generic;        // For List<T>
using System.Linq;                       // For LINQ ordering helpers
using UnityEngine;

// Central manager that turns player input (right-click) into orders for selected units.
// Issues Move orders (left-click+drag selection then right-click) and Attack orders (right-click on enemy).
// Supports formation placement, staggered offsets, and attack prioritization modes.
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;               // Singleton reference for easy access

    [Header("References")]
    public Camera mainCamera;                           // Main world camera used for raycasts
    public LayerMask groundLayerMask;                   // Layer mask used to detect ground clicks
    public LayerMask targetLayerMask;                   // Layer mask used to detect enemy/unit clicks

    [Header("Formation")]
    public FormationHelper.FormationType formation = FormationHelper.FormationType.Circle; // Default formation
    public float formationSpacing = 1.5f;               // Spacing between units in formation (world units)
    public float followSmoothing = 8f;                  // Smoothing for camera-follow movement if used in helpers

    [Header("Attack priority")]
    public enum AttackPriority { Closest, Weakest, HighestThreat } // Priority modes
    public AttackPriority attackPriority = AttackPriority.Closest  // Default priority mode
    ;

    private void Awake()
    {
        // Setup singleton
        if (Instance != null &&
