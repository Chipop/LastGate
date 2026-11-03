using System;
using UnityEngine;

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
    private Vector2 dir;
    private ShotParams shot;
    // How many times the projectile will pierce
    private float hitsLeft = 1;

    public event Action<Projectile> OnDone;

    public void Fire(Vector2 from, Vector2 direction, float speed, ShotParams shot)
    {
        transform.position = from;
        this.dir = direction.normalized;
        this.speed = speed;
        this.shot = shot;
        this.hitsLeft = 1 + shot.pierce;
        // alreadyHit.Clear();
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.position += (Vector3) dir * speed * Time.deltaTime;
        if(transform.position.y >= LimitLineY)
        {
            Done();
        }
    }

    // Unity Lifecycle
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Enemy enemy)) return;
        if (shot.slowPercent > 0f)
        {
            enemy.ApplySlow(shot.slowPercent, shot.slowSeconds);
        }
        enemy.Flinch(dir);                       // feel only: never push an enemy with a bolt
        enemy.TakeDamage(shot.damage);

        if (--hitsLeft <= 0)
        {
            Done();
        }
        
    }

    private void Done()
    {
        OnDone?.Invoke(this);
        gameObject.SetActive(false);
    }
}
