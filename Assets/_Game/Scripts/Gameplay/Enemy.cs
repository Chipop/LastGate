using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    private float wobbleSeed;
    private float hp;
    private float speed;
    private float gateDamage;
    private float xp;
    private float gateLineY;
    public event Action<Enemy> OnReachedGate;
    public event Action<Enemy> OnDied;

    public float Xp => xp;
    public float GateDamage => gateDamage;


    // Custom Methods
    public void Init(Vector2 spawnPos, float hp, float speed, int gateDamage, int xp, float size, Color tint, float gateLineY)
    {
        this.transform.position = spawnPos;
        this.hp = hp;
        this.speed = speed;
        this.gateDamage = gateDamage;
        this.xp = xp;
        this.gateLineY = gateLineY;

        transform.localScale = Vector3.one * size;
        sr.color = tint;
        gameObject.SetActive(true);
    }

    public void TakeDamage(float amount)
    {
        this.hp -= amount;
        if (hp <= 0)
        {
            OnDied?.Invoke(this);
            this.gameObject.SetActive(false);
        }
    }

    // Unity LifeCycle
    private void Awake()
    {
        wobbleSeed = UnityEngine.Random.value * 10f;
    }


    void Update()
    {
        float wobble = Mathf.Sin((Time.time + wobbleSeed) * 2f * Mathf.PI * 2f) * 0.15f;

        //transform.position += Vector3.down * speed * Time.deltaTime;
        transform.position += new Vector3(wobble * Time.deltaTime, -speed * Time.deltaTime, 0f);

        // Arrived at gate, then we make it disapear
        if (transform.position.y  <= gateLineY)
        {
            OnReachedGate?.Invoke(this);
            //gameObject.SetActive(false);
            //Destroy(gameObject);
        }
    }

}
