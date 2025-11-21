using UnityEngine;  

public class FogOfWarManager : MonoBehaviour

{
    public static FogOfWarManager Instance;                     // Singleton for global access

    [Header("Fog Settings")]
    public int resolution = 256;                                // Size of the fog texture (resolution x resolution)
    public float worldSize = 100f;                               // Width/height of map in world units
    public float revealFalloff = 5f;                             // Soft edge reveal falloff (smooth blending)

    [Header("Fog Colors")]
    public Color unexploredColor = Color.black;                  // Never seen before
    public Color exploredColor = new Color(0, 0, 0, 0.6f);        // Seen before but currently not visible
    public Color visibleColor = new Color(0, 0, 0, 0);            // Fully visible (transparent fog)

    private Texture2D fogTexture;                               // Texture used by fog material
    private Color[] fogPixels;                                  // Array storing fog color values
    private Renderer fogRenderer;                               // Renderer of fog plane (for updating material)

    private void Awake()
    {
        Instance = this;                                        // Initialize singleton
    }

    private void Start()
    {
        fogRenderer = GetComponent<Renderer>();                 // Get plane renderer with fog shader

        fogTexture = new Texture2D(resolution, resolution);     // Create fog texture
        fogTexture.wrapMode = TextureWrapMode.Clamp;            // Prevent repeating texture
        fogTexture.filterMode = FilterMode.Bilinear;            // Smooth edges

        fogPixels = new Color[resolution * resolution];         // Allocate pixel array

        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = unexploredColor;                     // Initialize all pixels as unexplored

        fogTexture.SetPixels(fogPixels);                        // Push pixel data to texture
        fogTexture.Apply();                                     // Upload texture to GPU

        fogRenderer.material.mainTexture = fogTexture;          // Assign texture to fog material
    }

    public void RevealArea(Vector3 worldPos, float radius)
    {
        Vector2Int center = WorldToFogCoords(worldPos);         // Convert world → fog texture coordinates
        int pixelRadius = Mathf.RoundToInt((radius / worldSize) * resolution); // Convert world radius → pixel radius

        for (int y = -pixelRadius; y <= pixelRadius; y++)       // Loop through Y area
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)   // Loop through X area
            {
                int px = center.x + x;                          // Calculate pixel X
                int py = center.y + y;                          // Calculate pixel Y

                if (px < 0 || py < 0 || px >= resolution || py >= resolution)
                    continue;                                   // Skip pixels outside texture bounds

                float dist = Mathf.Sqrt(x * x + y * y);         // Distance from center
                if (dist > pixelRadius) continue;               // Outside circle? skip

                int index = py * resolution + px;               // Convert 2D → 1D pixel index

                float t = dist / pixelRadius;                   // Normalized falloff
                Color blended = Color.Lerp(visibleColor, exploredColor, t * revealFalloff);

                fogPixels[index] = blended;                     // Write fog pixel
            }
        }

        fogTexture.SetPixels(fogPixels);                        // Push to texture
        fogTexture.Apply();                                     // Upload to GPU
    }

    Vector2Int WorldToFogCoords(Vector3 worldPos)
    {
        float half = worldSize / 2f;                            // Calculate half-size

        float tx = (worldPos.x + half) / worldSize;             // Normalize world x to [0,1]
        float tz = (worldPos.z + half) / worldSize;             // Normalize world z to [0,1]

        int px = Mathf.RoundToInt(tx * resolution);             // Convert to pixel X
        int py = Mathf.RoundToInt(tz * resolution);             // Convert to pixel Y

        return new Vector2Int(px, py);                          // Return as 2D coords
    }
}
