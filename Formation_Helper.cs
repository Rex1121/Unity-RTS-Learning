using System.Collections.Generic;
using UnityEngine;

// Small utility for computing formation positions for N units around a center point.
// Returns a List<Vector3> with positions in the same order every call (deterministic).
public static class FormationHelper
{
    // Supported basic formation shapes
    public enum FormationType { Circle, Line, Wedge }

    // Compute N formation positions around 'center'.
    // - center: world position acting as anchor
    // - count: number of positions to produce
    // - shape: formation shape
    // - spacing: approximate spacing between units in world units
    // Returns: ordered list of Vector3 positions
    public static List<Vector3> ComputeFormationPositions(Vector3 center, int count, FormationType shape, float spacing)
    {
        var results = new List<Vector3>(count);

        if (count <= 0) return results; // nothing to do

        switch (shape)
        {
            case FormationType.Circle:
                {
                    // Arrange units evenly around a circle. Radius scales with count & spacing.
                    float radius = Mathf.Max( spacing, spacing * Mathf.Sqrt(count) * 0.5f );
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / (float)count;
                        float ang = t * Mathf.PI * 2f; // angle in radians
                        Vector3 offset = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radius;
                        results.Add(center + offset);
                    }
                    break;
                }
            case FormationType.Line:
                {
                    // Arrange units in a line perpendicular to camera forward vector (so they face camera/player)
                    // Compute a forward vector (projected on XZ) from main camera to center to orient the line
                    Vector3 forward = Vector3.forward;
                    if (Camera.main != null)
                    {
                        forward = (center - Camera.main.transform.position);
                        forward.y = 0f;
                        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                        forward.Normalize();
                    }

                    // Right vector for line orientation
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                    // Center the line on the anchor
                    float totalWidth = (count - 1) * spacing;
                    Vector3 start = center - right * (totalWidth * 0.5f);

                    for (int i = 0; i < count; i++)
                    {
                        Vector3 pos = start + right * (i * spacing);
                        results.Add(pos);
                    }
                    break;
                }
            case FormationType.Wedge:
                {
                    // Arrange units in a wedge/cone facing the center forward direction.
                    // Rows: 1,2,3,... sized until we place all units.
                    int placed = 0;
                    int row = 0;
                    float forwardStep = spacing * 0.9f;
                    if (Camera.main != null)
                    {
                        // We want wedge to face camera direction so visual makes sense
                        Vector3 forward = (center - Camera.main.transform.position);
                        forward.y = 0f;
                        forward.Normalize();
                        // We will offset positions along negative forward to get in front of center
                    }
                    while (placed < count)
                    {
                        row++;
                        int itemsInRow = row;
                        float rowOffset = (itemsInRow - 1) * spacing * 0.5f;
                        for (int i = 0; i < itemsInRow && placed < count; i++)
                        {
                            // Move forward for each completed row to shape a wedge
                            float z = -(row - 1) * forwardStep;
                            float x = (i * spacing) - rowOffset;
                            Vector3 pos = center + new Vector3(x, 0f, z);
                            results.Add(pos);
                            placed++;
                        }
                    }
                    break;
                }
            default:
                {
                    // Fallback: place all units at the center to avoid empty results
                    for (int i = 0; i < count; i++) results.Add(center);
                    break;
                }
        }

        return results;
    }
}
