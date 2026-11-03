using System;
using System.Collections.Generic;
using UnityEngine;

public class BombLauncher : MonoBehaviour
{
    [SerializeField] private MageController mage;
    [SerializeField] private TargetSelector targetSelector;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bomb bombPrefab;
    [SerializeField] private float interval = 4f;
    [SerializeField] private float bombSpeed = 14f;
    [SerializeField] private float baseDamage = 40f;
    [SerializeField] private float damagePerExtraLevel = 20f;
    [SerializeField] private float baseRadius = 2f;
    [SerializeField] private float radiusPerExtraLevel = 0.4f;
    [SerializeField] private float blastKnockback = 0.6f;   // units, scaled by each enemy's knockback factor
    public event Action<Vector2, int> OnBlast;               // (centre, enemies hit): the feel layer listens, the launcher stays pure
    private float timer;
    private List<Enemy> victims = new List<Enemy>();

    // Custom Methods
    public float Interval => interval;
    public float DamageAt(int level) => baseDamage + damagePerExtraLevel * (level - 1);
    public float RadiusAt(int level) => baseRadius + radiusPerExtraLevel * (level - 1);

    private void Throw()
    {
        Enemy target = targetSelector.GetBestTarget();
        if (target == null)
        {
            // nobody to bomb: try again next frame
            timer = interval;
            return;
        }

        int level = mage.BombLevel;
        float damage = DamageAt(level);
        float radius = RadiusAt(level);

        Bomb bomb = Instantiate(bombPrefab);
        bomb.Launch(muzzle.position, target.transform.position, bombSpeed, radius,
            center => Blast(center, damage, radius));
    }

    private void Blast(Vector2 center, float damage, float radius)
    {
        float sqrRadius = radius * radius;

        victims.Clear();
        foreach (Enemy enemy in enemyPool.Live)
        {
            float sqrDistance = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (sqrDistance <= sqrRadius) victims.Add(enemy);
        }

        // Damage AFTER the search: a dying enemy leaves the live list,
        // and changing a list while a foreach reads it throws an error
        foreach (Enemy enemy in victims)
        {
            enemy.Push((Vector2)enemy.transform.position - center, blastKnockback);
            enemy.TakeDamage(damage);
        }
        OnBlast?.Invoke(center, victims.Count);
        Analytics.Track("bomb_blast", new() { ["enemies_hit"] = victims.Count });
    }

    // Unity Lifecycle
    private void Update()
    {
        if (mage.BombLevel <= 0) return;

        timer += Time.deltaTime;
        if (timer < interval) return;

        timer = 0f;
        Throw();
    }
}