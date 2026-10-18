using UnityEngine;

public class MageController : MonoBehaviour
{
    [SerializeField] private MageStats baseStats;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    private float fireRate;
    private float cooldown = 1f;
    private float damage;
    private float projectileSpeed;
    private int projectiles;
    private int pierce;

    public float Damage => damage;
    public float FireRate => fireRate;
    public float Projectiles => projectiles;
    public int Pierce => pierce;

    // Custom Methods
    private Enemy GetNearestEnemy()
    {
        Enemy nearestEnemy = null;
        float nearestSqrDistance = float.MaxValue;
        foreach (Enemy enemy in enemyPool.Live)
        {
            float sqrDistance = ((Vector2)enemy.transform.position - (Vector2)muzzle.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    private void FireProjectile(Enemy nearestEnemy)
    {
        Vector3 direction = (nearestEnemy.transform.position - muzzle.position).normalized;
        for (int i = 0; i < projectiles; i++)
        {
            float angle = (i - (projectiles - 1) / 2f) * 8f;        // centred fan: -8, 0, +8 for projectiles = 3
            Vector2 d = Quaternion.Euler(0, 0, angle) * direction;
            Projectile p = projectilePool.Get();
            p.Fire(muzzle.position, d, projectileSpeed, Damage, Pierce);
        }
    }

    // Unity Lifecycle

    private void Awake()
    {
        damage = baseStats.damage;
        fireRate = baseStats.fireRate;
        projectileSpeed = baseStats.projectileSpeed;
        projectiles = baseStats.projectiles;
        pierce = baseStats.pierce;
    }

    private void Update()
    {
        cooldown -= Time.deltaTime;
        Enemy nearestEnemy = GetNearestEnemy();
        if (cooldown > 0 || nearestEnemy == null) return;

        cooldown = 1f / fireRate;
    
        if (nearestEnemy != null)
        {
            FireProjectile(nearestEnemy);
        }
    }


}
