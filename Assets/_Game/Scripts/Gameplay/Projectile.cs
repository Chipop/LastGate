using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public struct ShotParams
    {
        public float damage;
        public int pierce;
        public float slowPercent; // 0 = no slow (Frost not owned)
        public float slowSeconds;
    }

    [SerializeField] float speed = 3f;
    [SerializeField] float LimitLineY = 12f;
    private static int lastPhysicsSyncFrame = -1;
    private readonly List<RaycastHit2D> contacts = new(8);
    private readonly HashSet<(Enemy enemy, int spawn)> alreadyHit = new();
    private CircleCollider2D hitCollider;
    private ContactFilter2D enemyFilter;
    private Vector2 dir;
    private ShotParams shot;
    private int hitsLeft;

    public event Action<Projectile> OnDone;

    private void Awake()
    {
        hitCollider = GetComponent<CircleCollider2D>();
        enemyFilter = new ContactFilter2D { useTriggers = true };
        enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
    }

    public void Fire(Vector2 from, Vector2 direction, float speed, ShotParams shot)
    {
        transform.position = from;
        this.dir = direction.normalized;
        this.speed = speed;
        this.shot = shot;
        this.hitsLeft = 1 + shot.pierce;
        alreadyHit.Clear();
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        Advance(speed * Time.deltaTime);
    }

    private void Advance(float distance)
    {
        if (distance <= 0f || dir == Vector2.zero) return;

        // Enemies move in Update. Sync their colliders once before this frame's casts.
        if (lastPhysicsSyncFrame != Time.frameCount)
        {
            Physics2D.SyncTransforms();
            lastPhysicsSyncFrame = Time.frameCount;
        }

        Vector2 origin = transform.TransformPoint(hitCollider.offset);
        Vector3 scale = transform.lossyScale;
        float radius = hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

        // Sweep the bolt's full path and spend piercing hits in travel order.
        int contactCount = Physics2D.CircleCast(origin, radius, dir, enemyFilter, contacts, distance);
        for (int i = 0; i < contactCount; i++)
        {
            Collider2D collider = contacts[i].collider;
            if (collider == null || !collider.TryGetComponent(out Enemy enemy) ||
                !enemy.isActiveAndEnabled || enemy.Hp <= 0f ||
                !alreadyHit.Add((enemy, enemy.SpawnVersion))) continue;

            if (shot.slowPercent > 0f)
                enemy.ApplySlow(shot.slowPercent, shot.slowSeconds);

            enemy.Flinch(dir);
            enemy.TakeDamage(shot.damage);
            if (--hitsLeft <= 0)
            {
                Done();
                return;
            }
        }

        transform.position += (Vector3)(dir * distance);
        if (transform.position.y >= LimitLineY) Done();
    }

    private void Done()
    {
        OnDone?.Invoke(this);
        gameObject.SetActive(false);
    }
}
