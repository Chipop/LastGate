using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    // Custom Methods
    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
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
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // Unity Lifecycle
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // rotation, window resize
        if (Screen.safeArea != lastSafeArea) Apply();
    }
}
