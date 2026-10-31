using System.Collections.Generic;
using UnityEngine;

public class MageController : MonoBehaviour
{
    [SerializeField] private float singleTargetSpreadDegrees = 2f;
    [SerializeField] private MageStats baseStats;
    [SerializeField] private TargetSelector targetSelector;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private Transform muzzle;
    public float FireRate { get; set; }
    public float Cooldown { get; set; } = 1f;
    public float ProjectileSpeed { get; set; }
    public float Damage { get; set; }
    public int Projectiles { get; set; }
    public int Pierce { get; set; }
    public int ChainLevel { get; set; }
    public int FrostLevel { get; set; }

    // Custom Methods

    private Vector2 GetAimPoint(Enemy target)
    {
        Vector2 targetPosition = target.transform.position;
        Vector2 relativePosition = targetPosition - (Vector2)muzzle.position;

        if (!AimMath.TryGetInterceptTime(relativePosition, target.Velocity, ProjectileSpeed, out float time))
        {
            // no solution: aim straight at it
            return targetPosition;
        }

        Vector2 aimPoint = targetPosition + target.Velocity * time;
        // The enemy stops at the gate line; never aim below it.
        aimPoint.y = Mathf.Max(aimPoint.y, target.GateLineY);
        return aimPoint;
    }

    private void FireAt(Enemy target, float angleOffsetDegrees)
    {
        Vector2 aimPoint = GetAimPoint(target);
        Vector2 direction = (aimPoint - (Vector2)muzzle.position).normalized;
        direction = Quaternion.Euler(0f, 0f, angleOffsetDegrees) * direction;

        Projectile projectile = projectilePool.Get();
        projectile.Fire(muzzle.position, direction, ProjectileSpeed, Damage, Pierce);
    }

    // Returns false when there was nobody to shoot at (so the cooldown is not spent).
    private bool FireVolley()
    {
        List<Enemy> targets = targetSelector.GetBestTargets(Projectiles);
        if (targets.Count == 0) return false;

        for (int i = 0; i < Projectiles; i++)
        {
            if (targets.Count > 1)
            {
                // Several enemies: bolt i goes to enemy i (round-robin if there are more bolts than enemies)
                FireAt(targets[i % targets.Count], 0f);
            }
            else
            {
                // One enemy: a small fan, centred on it
                float angle = (i - (Projectiles - 1) / 2f) * singleTargetSpreadDegrees;
                FireAt(targets[0], angle);
            }
        }
        return true;
    }

    // private void FireProjectile(Enemy nearestEnemy)
    // {
    //     Vector3 direction = ((Vector3)GetAimPoint(nearestEnemy) - muzzle.position).normalized;
    //     for (int i = 0; i < Projectiles; i++)
    //     {
    //         // centred fan: -8, 0, +8 for projectiles = 3
    //         float angle = (i - (Projectiles - 1) / 2f) * 8f;        
    //         Vector2 d = Quaternion.Euler(0, 0, angle) * direction;
    //         Projectile p = projectilePool.Get();
    //         p.Fire(muzzle.position, d, ProjectileSpeed, Damage, Pierce);
    //     }
    // }

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
        Enemy nearestEnemy = targetSelector.GetBestTarget();
        if (Cooldown > 0 || nearestEnemy == null) return;

        Cooldown = 1f / FireRate;
    
        if (nearestEnemy != null)
        {
            FireVolley();
        }
    }

}
