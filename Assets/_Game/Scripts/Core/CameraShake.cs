using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 restPosition;
    private Coroutine running;

    // Custom Methods
    public void Shake(float amplitude, float seconds)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShakeRoutine(amplitude, seconds));
    }

    private IEnumerator ShakeRoutine(float amplitude, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {   // keeps shaking even if the game is paused
            elapsed += Time.unscaledDeltaTime;
            float strength = amplitude * (1f - elapsed / seconds);
            Vector2 offset = Random.insideUnitCircle * strength;
            transform.position = restPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }
        transform.position = restPosition;
        running = null;
    }

    // Unity Lifecycle
    private void Awake()
    {
        restPosition = transform.position;
    }
}
