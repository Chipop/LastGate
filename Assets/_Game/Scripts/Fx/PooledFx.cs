using UnityEngine;
using UnityEngine.Pool;

public class PooledFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    private IObjectPool<PooledFx> pool;

    // Custom Methods
    public void SetPool(IObjectPool<PooledFx> ownerPool)
    {
        pool = ownerPool;
    }

    public void Play(Vector3 position, Color color)
    {
        transform.position = position;
        // struct: copy, modify, the module writes through
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        particles.Play();
    }

    // Unity calls this when the particle system finishes (Stop Action = Callback)
    private void OnParticleSystemStopped()
    {
        pool.Release(this);
    }
}
