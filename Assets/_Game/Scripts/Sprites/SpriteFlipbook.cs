using UnityEngine;

public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;
    private float timeOffset;

    // Custom Methods
    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        // so a crowd of enemies doesn't animate in lockstep
        timeOffset = Random.value * 10f;
    }

    // Unity Lifecycle
    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        // Time.time is scaled: the animation pauses with the game and speeds up with x2
        int frameIndex = (int)((Time.time + timeOffset) * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
