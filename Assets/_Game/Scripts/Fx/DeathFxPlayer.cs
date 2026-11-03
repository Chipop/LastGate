using UnityEngine;
using UnityEngine.Pool;

public class DeathFxPlayer : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private PooledFx fxPrefab;
    private ObjectPool<PooledFx> pool;

    // Custom Methods
    private PooledFx CreateFx()
    {
        PooledFx fx = Instantiate(fxPrefab, transform);
        fx.SetPool(pool);
        return fx;
    }

    private void Spawner_OnEnemyKilled(Enemy enemy)
    {
        PooledFx fx = pool.Get();
        fx.Play(enemy.transform.position, enemy.Data.deathColor);
    }

    // Unity Lifecycle
    private void Awake()
    {
        pool = new ObjectPool<PooledFx>(
            createFunc: CreateFx,
            actionOnGet: fx => fx.gameObject.SetActive(true),
            actionOnRelease: fx => fx.gameObject.SetActive(false),
            actionOnDestroy: fx => Destroy(fx.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 40);
    }

    private void Start()
    {
        spawner.OnEnemyKilled += Spawner_OnEnemyKilled;
    }
}
