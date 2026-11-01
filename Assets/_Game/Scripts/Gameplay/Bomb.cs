using System;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private float blastSeconds = 0.2f;
    private Vector2 target;
    private float speed;
    private float radius;
    private bool exploded;
    private float blastTimer;
    private Action<Vector2> onExploded;

    // Custom Methods
    public void Launch(Vector2 from, Vector2 target, float speed, float radius, Action<Vector2> onExploded)
    {
        transform.position = from;
        this.target = target;
        this.speed = speed;
        this.radius = radius;
        this.onExploded = onExploded;
    }

    private void Explode()
    {
        exploded = true;
        // the circle IS the blast area: diameter = 2 x radius
        transform.localScale = Vector3.one * radius * 2f;
        onExploded?.Invoke(target);
    }

    // Unity Lifecycle
    private void Update()
    {
        if (!exploded)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((Vector2)transform.position == target) Explode();
            return;
        }

        blastTimer += Time.deltaTime;
        if (blastTimer >= blastSeconds) Destroy(gameObject);
    }
}