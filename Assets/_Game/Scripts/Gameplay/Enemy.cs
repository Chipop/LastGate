using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color frostTint = new Color(0.5f, 0.8f, 1f);
    private Color baseTint;
    // 1 = normal speed, 0.7 = 30 % slower 
    private float slowMultiplier = 1f;
    private float slowTimer;
    public string Id { get; private set; }
    public EnemyData Data { get; private set; }
    private float wobbleSeed;
    private float hp;
    private float speed;
    private float attackDamage;
    private float attackInterval;
    private float attackCooldown = 0f;
    private int xp;
    private float gateLineY;
    public event Action<Enemy> OnReachedGate;
    public event Action<Enemy> OnDied;
    public struct AttackParams { public float attackDamage; }
    public event Action<AttackParams> OnAttack;

    public float Xp => xp;
    public float Hp => hp;
    public float GateLineY => gateLineY;
    public bool IsAtGate => transform.position.y <= gateLineY;
    public Vector2 Velocity
    {
        get
        {
            if(IsAtGate) return Vector2.zero;
            return new Vector2(0, -speed * slowMultiplier);
        }
    }

    // Custom Methods
    public void Init(EnemyData data, Vector2 spawnPos, float hp, float speed, float attackDamage, float attackInterval , int xp, float size, Color tint, float gateLineY)
    {
        this.Data = data;
        this.Id = data.id;
        this.transform.position = spawnPos;
        this.hp = hp;
        this.speed = speed;
        this.attackDamage = attackDamage;
        this.attackInterval = attackInterval;
        this.xp = xp;
        this.gateLineY = gateLineY;
        //reset cooldown for pooled enemies
        this.attackCooldown = 0f;
        baseTint = tint;
        slowMultiplier = 1f;
        slowTimer = 0f;
        sr.color = tint;

        transform.localScale = Vector3.one * size;
        gameObject.SetActive(true);
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f) return;

        this.hp -= amount;
        if (hp <= 0)
        {
            OnDied?.Invoke(this);
            this.gameObject.SetActive(false);
        }
    }

    private void Attack()
    {
        OnAttack?.Invoke(new AttackParams { attackDamage = this.attackDamage });
    }

    public void ApplySlow(float percent, float seconds)
    {
        float newMultiplier = 1f - percent;
        // A stronger slow replaces a weaker one; the same slow just restarts the timer
        slowMultiplier = slowTimer > 0f ? Mathf.Min(slowMultiplier, newMultiplier) : newMultiplier;
        slowTimer = seconds;
        sr.color = frostTint;
    }

    // Unity LifeCycle
    private void Awake()
    {
        wobbleSeed = UnityEngine.Random.value * 10f;
    }


    void Update()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
                sr.color = baseTint;
            }
        }

        if (IsAtGate)
        {
            attackCooldown -= Time.deltaTime * slowMultiplier;
            if (attackCooldown <= 0)
            {
                Attack();
                attackCooldown = attackInterval;
            }
            return; // Do not advance
        }

        float wobble = Mathf.Sin((Time.time + wobbleSeed) * 2f * Mathf.PI * 2f) * 0.15f;

        //transform.position += Vector3.down * speed * Time.deltaTime;
        float newSpeed = speed * slowMultiplier;
        transform.position += new Vector3(wobble * Time.deltaTime, -newSpeed * Time.deltaTime, 0f);

    }

}
