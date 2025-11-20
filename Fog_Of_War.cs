using UnityEngine;                         
using System.Collections.Generic;          
public class FogOfWar : MonoBehaviour
{
    public int textureSize = 512;           // Resolution of the fog mask texture
    public float worldSize = 200f;          // Size of the game world to map the fog over
    public Material fogMaterial;            // Material that receives the fog texture

    private Texture2D fogTexture;           // Final fog-of-war texture
    private Color32[] fogPixels;            // Pixel buffer for fog values
    private Color32[] clearPixels;          // Pre-filled buffer (full fog)
    private List<FogUnit> visionUnits;      // List of all units providing vision

    public static FogOfWar Instance;        // Static instance lookup

    private void Awake()
    {
        Instance = this;                    // Store singleton reference
        visionUnits = new List<FogUnit>();  // Initialize list of vision sources

        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
        fogTexture.filterMode = FilterMode.Bilinear; // Smooth fog edges

        fogPixels = new Color32[textureSize * textureSize]; // Allocate pixel buffer
        clearPixels = new Color32[textureSize * textureSize]; // Allocate clear buffer

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color32(0, 0, 0, 255); // Fully fogged pixel

        fogTexture.SetPixels32(clearPixels);   // Fill texture with fog
        fogTexture.Apply();                    // Apply texture changes

        fogMaterial.SetTexture("_FogTex", fogTexture); // Assign texture to material
    }

    private void LateUpdate()
    {
        UpdateFog(); // Recalculate fog every frame
    }

    public void RegisterUnit(FogUnit unit)
    {
        visionUnits.Add(unit); // Add vision unit to system
    }

    public void UnregisterUnit(FogUnit unit)
    {
        visionUnits.Remove(unit); // Remove unit when it dies/despawns
    }

    void UpdateFog()
    {
        fogPixels = (Color32[])clearPixels.Clone(); // Reset fog each frame

        foreach (var unit in visionUnits) // Loop through all vision units
        {
            RevealArea(unit.transform.position, unit.viewRadius); // Reveal vision circle
        }

        fogTexture.SetPixels32(fogPixels); // Push pixel buffer to texture
        fogTexture.Apply();                // Apply texture changes
    }

    void RevealArea(Vector3 worldPos, float radius)
    {
        Vector2 texPos = WorldToFogCoord(worldPos); // Convert world → texture coordinate

        int radiusTex = Mathf.RoundToInt((radius / worldSize) * textureSize); // Convert radius to tex-space

        for (int y = -radiusTex; y < radiusTex; y++) // Loop over a square of pixels
        {
            for (int x = -radiusTex; x < radiusTex; x++)
            {
                int px = (int)texPos.x + x; // Current pixel X
                int py = (int)texPos.y + y; // Current pixel Y

                if (px < 0 || py < 0 || px >= textureSize || py >= textureSize)
                    continue; // Skip pixels outside bounds

                float dist = Mathf.Sqrt(x * x + y * y); // Distance from center

                if (dist < radiusTex) // Inside vision circle
                {
                    int idx = py * textureSize + px; // Convert 2D → 1D index
                    fogPixels[idx] = new Color32(0, 0, 0, 0); // Mark as visible (no fog)
                }
            }
        }
    }

    Vector2 WorldToFogCoord(Vector3 worldPosition)
    {
        float normX = (worldPosition.x / worldSize) + 0.5f; // Convert world → 0–1 range
        float normZ = (worldPosition.z / worldSize) + 0.5f;

        return new Vector2(normX * textureSize, normZ * textureSize); // Convert to texture space
    }
}
