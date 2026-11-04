using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    // Custom Methods
    private void Apply()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        // Anchors are fractions (0..1) of the parent; the safe area is in pixels
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }

    // Unity Lifecycle
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // rotation, window resize
        if (Screen.safeArea != lastSafeArea) Apply();
    }
}
