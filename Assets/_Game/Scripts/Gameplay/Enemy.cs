using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private CircleCollider2D hitCollider;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color hitTint = new Color(1f, 0.35f, 0.35f);
    private float hitFlashTimer;
    [SerializeField] private Color frostTint = new Color(0.5f, 0.8f, 1f);
    private Color baseTint;
    // 1 = normal speed, 0.7 = 30 % slower 
    private float slowMultiplier = 1f;
    private float slowTimer;
    [SerializeField] private float pushDrag = 12f;   // how fast a push dies out; distance travelled = impulse / drag
    private Vector2 pushVelocity;
    [SerializeField] private float flinchDistance = 0.125f;   // 2 art pixels at 16 PPU
    [SerializeField] private float flinchSeconds = 0.08f;
    private Vector2 flinchOffset;
    private float flinchTimer;
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
        hitFlashTimer = 0f;
        pushVelocity = Vector2.zero;
        flinchOffset = Vector2.zero;
        flinchTimer = 0f;
        baseTint = tint;
        RefreshTint();

        flipbook.SetFrames(data.frames);
        hitCollider.radius = data.hitRadius;

        gameObject.SetActive(true);
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f) return;

        this.hp -= amount;

        hitFlashTimer = 0.08f;
        RefreshTint();

        if (hp <= 0)
        {
            OnDied?.Invoke(this);
            this.gameObject.SetActive(false);
        }
    }

    private void RefreshTint()
    {
        if (hitFlashTimer > 0f) sr.color = hitTint;
        else if (slowTimer > 0f) sr.color = frostTint;
        else sr.color = baseTint;
    }

    private void Attack()
    {
        OnAttack?.Invoke(new AttackParams { attackDamage = this.attackDamage });
    }

    // A visual flinch: the enemy is drawn 2 px along `direction` for a few frames, then exactly back.
    // Net movement is zero, so it changes nothing about when the enemy reaches the gate.
    public void Flinch(Vector2 direction)
    {
        if (direction == Vector2.zero) return;
        transform.position -= (Vector3)flinchOffset;          // undo a flinch still in progress
        flinchOffset = direction.normalized * flinchDistance;
        transform.position += (Vector3)flinchOffset;
        flinchTimer = flinchSeconds;
    }

    // Shove the enemy `distance` units along `direction` (scaled by its data); the push fades out over a few frames.
    // Used by the fireball blast only: a push IS gameplay (it buys time), so it stays rare.
    public void Push(Vector2 direction, float distance)
    {
        float d = distance * Data.knockback;
        if (d <= 0f || direction == Vector2.zero) return;
        pushVelocity += direction.normalized * d * pushDrag;
    }

    public void ApplySlow(float percent, float seconds)
    {
        float newMultiplier = 1f - percent;
        // A stronger slow replaces a weaker one; the same slow just restarts the timer
        slowMultiplier = slowTimer > 0f ? Mathf.Min(slowMultiplier, newMultiplier) : newMultiplier;
        slowTimer = seconds;
        RefreshTint();
    }

    // Unity LifeCycle
    private void Awake()
    {
        wobbleSeed = UnityEngine.Random.value * 10f;
    }


    void Update()
    {
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f) RefreshTint();
        }
        else if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
                RefreshTint();
            }
        }

        if (flinchTimer > 0f)
        {
            flinchTimer -= Time.deltaTime;
            if (flinchTimer <= 0f)
            {
                transform.position -= (Vector3)flinchOffset;
                flinchOffset = Vector2.zero;
            }
        }

        if (pushVelocity != Vector2.zero)
        {
            transform.position += (Vector3)(pushVelocity * Time.deltaTime);
            pushVelocity *= Mathf.Exp(-pushDrag * Time.deltaTime);
            if (pushVelocity.sqrMagnitude < 0.0001f) pushVelocity = Vector2.zero;
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
