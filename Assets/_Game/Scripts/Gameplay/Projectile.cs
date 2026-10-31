using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 3f;
    [SerializeField] float LimitLineY = 12f;
    private Vector2 dir;
    private float damage;
    // How many times the projectile will pierce
    private float hitsLeft = 1;

    public event Action<Projectile> OnDone;

    public void Fire(Vector2 from, Vector2 direction, float speed, float damage, int pierce)
    {
        transform.position = from;
        this.dir = direction.normalized;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        this.speed = speed;
        this.damage = damage;
        this.hitsLeft = 1 + pierce;
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
        if (!other.TryGetComponent(out Enemy e)) return;
        e.TakeDamage(damage);

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
