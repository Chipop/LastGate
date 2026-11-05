using UnityEngine;
using UnityEngine.UI;

// Keep the entire authored portrait layout visible, even in a wide browser.
// Extra space expands the canvas instead of cropping the bottom of menus.
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class ResponsiveCanvas : MonoBehaviour
{
    private Vector2 designSize = new Vector2(1080, 1920);
    private CanvasScaler scaler;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public static void Configure(Canvas canvas, Vector2 referenceSize)
    {
        ResponsiveCanvas responsive = canvas.GetComponent<ResponsiveCanvas>();
        if (responsive == null) responsive = canvas.gameObject.AddComponent<ResponsiveCanvas>();
        responsive.designSize = referenceSize;
        responsive.scaler = canvas.GetComponent<CanvasScaler>();
        responsive.Apply();
    }

    public static void FitToSafeArea(GameObject panel)
    {
        if (panel.GetComponent<SafeAreaFitter>() == null)
            panel.AddComponent<SafeAreaFitter>();
    }

    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) return;

        if (scaler == null) scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // Reserve room for notches/insets before calculating the fit scale.
        // A safe-area panel will consequently retain at least designSize units.
        scaler.referenceResolution = new Vector2(
            designSize.x * Screen.width / safe.width,
            designSize.y * Screen.height / safe.height);
        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void Update()
    {
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height ||
            lastSafeArea != Screen.safeArea)
            Apply();
    }
}
