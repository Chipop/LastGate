using UnityEngine;

public class AudioService : MonoBehaviour
{
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource[] sfxSources;
    [SerializeField] private float pitchVariation = 0.08f;
    private int nextSource;

    private const string MutedKey = "muted";

    public bool IsMuted => AudioListener.volume == 0f;

    // Custom Methods
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = sfxSources[nextSource];
        // round-robin: pitch is per source
        nextSource = (nextSource + 1) % sfxSources.Length;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }

    // Browsers refuse to play audio before the first user gesture: call this from the Title tap
    public void StartMusic()
    {
        if (!music.isPlaying) music.Play();
    }

    public void ToggleMute()
    {
        AudioListener.volume = IsMuted ? 1f : 0f;
        PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
    }

    // Unity Lifecycle
    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetInt(MutedKey, 0) == 1 ? 0f : 1f;
    }
}
