using UnityEngine;

public static class AimMath
{
    // Time at which a projectile fired from the origin at projectileSpeed meets a target that starts at
    // relativePosition and moves with targetVelocity. Returns false when there is no meeting point.
    public static bool TryGetInterceptTime(Vector2 relativePosition, Vector2 targetVelocity, float projectileSpeed, out float time)
    {
        time = 0f;

        float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector2.Dot(relativePosition, targetVelocity);
        float c = Vector2.Dot(relativePosition, relativePosition);

        // Same speed as the target: the t² term disappears, the equation is linear: b·t + c = 0
        if (Mathf.Abs(a) < 0.0001f)
        {
            if (Mathf.Abs(b) < 0.0001f) return false;
            time = -c / b;
            return time > 0f;
        }

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false;

        float root = Mathf.Sqrt(discriminant);
        float t1 = (-b - root) / (2f * a);
        float t2 = (-b + root) / (2f * a);
        float smaller = Mathf.Min(t1, t2);
        float larger = Mathf.Max(t1, t2);

        // We want the first meeting in the future: the smallest positive root.
        if (smaller > 0f) { time = smaller; return true; }
        if (larger > 0f) { time = larger; return true; }
        return false;
    }
}
