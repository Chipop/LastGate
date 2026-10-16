using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    private Stack<Projectile> free;
    private int prewarm = 100;

    private Projectile Make()
    {
        Projectile p = Instantiate(projectilePrefab, Vector2.zero, Quaternion.identity);
        p.gameObject.SetActive(false);
        p.OnDone += P_OnDone;
        return p;
    }

    public Projectile Get()
    {
        //Debug.Log("Free Count " + free.Count);
        Projectile p = free.Count > 0 ? free.Pop() : Make();
        return p;
    }
   
    private void P_OnDone(Projectile p)
    {
        free.Push(p);
    }

    // Unity Lifecycle

    private void Awake()
    {
        free = new Stack<Projectile>();
        for (int i = 0; i < prewarm; i++)
        {
            free.Push(Make());
        }
    }
}
