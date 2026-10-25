using UnityEngine;

public class MageController : MonoBehaviour
{
    [SerializeField] private MageStats baseStats;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    public float FireRate { get; set; }
    public float Cooldown { get; set; } = 1f;
    public float ProjectileSpeed { get; set; }
    public float Damage { get; set; }
    public float Projectiles { get; set; }
    public int Pierce { get; set; }
    public int ChainLevel { get; set; }
    public int FrostLevel { get; set; }

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
        for (int i = 0; i < Projectiles; i++)
        {
            float angle = (i - (Projectiles - 1) / 2f) * 8f;        // centred fan: -8, 0, +8 for projectiles = 3
            Vector2 d = Quaternion.Euler(0, 0, angle) * direction;
            Projectile p = projectilePool.Get();
            p.Fire(muzzle.position, d, ProjectileSpeed, Damage, Pierce);
        }
    }

    // Unity Lifecycle

    private void Awake()
    {
        Damage = baseStats.damage;
        FireRate = baseStats.fireRate;
        ProjectileSpeed = baseStats.projectileSpeed;
        Projectiles = baseStats.projectiles;
        Pierce = baseStats.pierce;
    }

    private void Update()
    {
        Cooldown -= Time.deltaTime;
        Enemy nearestEnemy = GetNearestEnemy();
        if (Cooldown > 0 || nearestEnemy == null) return;

        Cooldown = 1f / FireRate;
    
        if (nearestEnemy != null)
        {
            FireProjectile(nearestEnemy);
        }
    }


}
