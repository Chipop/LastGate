using System.Collections;
using UnityEngine;

public class GateFeedback : MonoBehaviour
{
    [SerializeField] private Gate gate;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private CanvasGroup damageVignette;
    [SerializeField] private float minSecondsBetweenFeedback = 0.3f;
    [SerializeField] private float shakeAmplitude = 0.15f;
    [SerializeField] private float shakeSeconds = 0.12f;
    private float previousHp;
    private float lastFeedbackTime = -10f;
    private Coroutine vignetteRoutine;

    // Custom Methods
    private void Gate_OnHpChanged(Gate.OnHpChangedEventArgs args)
    {
        bool tookDamage = args.hp < previousHp;
        previousHp = args.hp;
        if (!tookDamage) return;

        // Unscaled time: a throttle measured in game time would stop working while the game is paused
        if (Time.unscaledTime - lastFeedbackTime < minSecondsBetweenFeedback) return;
        lastFeedbackTime = Time.unscaledTime;

        cameraShake.Shake(shakeAmplitude, shakeSeconds);
        if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
        vignetteRoutine = StartCoroutine(FlashVignette(0.15f));
    }

    private IEnumerator FlashVignette(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            damageVignette.alpha = 1f - elapsed / seconds;
            yield return null;
        }
        damageVignette.alpha = 0f;
    }

    // Unity Lifecycle
    private void Start()
    {
        previousHp = gate.Hp;
        gate.OnHpChanged += Gate_OnHpChanged;
    }
}
