using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
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
            return new Vector2(0, -speed);
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

        transform.localScale = Vector3.one * size;
        sr.color = tint;
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

    // Unity LifeCycle
    private void Awake()
    {
        wobbleSeed = UnityEngine.Random.value * 10f;
    }


    void Update()
    {
        if (IsAtGate)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0)
            {
                Attack();
                attackCooldown = attackInterval;
            }
            return; // Do not advance
        }

        float wobble = Mathf.Sin((Time.time + wobbleSeed) * 2f * Mathf.PI * 2f) * 0.15f;

        //transform.position += Vector3.down * speed * Time.deltaTime;
        transform.position += new Vector3(wobble * Time.deltaTime, -speed * Time.deltaTime, 0f);

    }

}
