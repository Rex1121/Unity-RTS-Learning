using UnityEngine;
using UnityEngine.UI;  

public class SelectionBoxUI : MonoBehaviour
{
    public Image boxImage;  // UI Image used as rectangle

    private void Start()
    {
        Hide();             // Hide until needed
    }

    public void UpdateRectangle(Rect rect)
    {
        if (!boxImage.enabled) boxImage.enabled = true; // Ensure it's visible

        boxImage.rectTransform.anchoredPosition =
            new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2); // Set center

        boxImage.rectTransform.sizeDelta = new Vector2(rect.width, rect.height); // Set size
    }

    public void Hide()
    {
        boxImage.enabled = false; // Simply hide the box
    }
}
