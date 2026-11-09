using UnityEngine;
using UnityEngine.Pool;

// The feel of a fireball blast: shake, a short freeze, a burst. BombLauncher doesn't know any of this exists.
public class BombFeedback : MonoBehaviour
{
    [SerializeField] private BombLauncher launcher;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private PooledFx blastFxPrefab;
    [SerializeField] private Color blastColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float shakeAmplitude = 0.25f;
    [SerializeField] private float shakeSeconds = 0.2f;
    [SerializeField] private float hitStopSeconds = 0.06f;
    private ObjectPool<PooledFx> pool;

    // Custom Methods
    private PooledFx CreateFx()
    {
        PooledFx fx = Instantiate(blastFxPrefab, transform);
        fx.SetPool(pool);
        return fx;
    }

    private void Launcher_OnBlast(Vector2 center, int victims)
    {
        pool.Get().Play(center, blastColor);
        cameraShake.Shake(shakeAmplitude, shakeSeconds);
        if (victims > 0) gameManager.HitStop(hitStopSeconds);   // a blast that hits nothing shouldn't freeze the game
    }

    // Unity Lifecycle
    private void Awake()
    {
        pool = new ObjectPool<PooledFx>(
            createFunc: CreateFx,
            actionOnGet: fx => fx.gameObject.SetActive(true),
            actionOnRelease: fx => fx.gameObject.SetActive(false),
            actionOnDestroy: fx => { if (fx != null) Destroy(fx.gameObject); },
            collectionCheck: false,
            defaultCapacity: 4,
            maxSize: 8);
    }

    private void Start()
    {
        launcher.OnBlast += Launcher_OnBlast;
    }

    private void OnDestroy()
    {
        if (launcher != null) launcher.OnBlast -= Launcher_OnBlast;
        pool?.Dispose();
    }
}
