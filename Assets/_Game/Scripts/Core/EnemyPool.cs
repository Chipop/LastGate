using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    private Stack<Enemy> free;
    private List<Enemy> live;
    // For the mage to read it and target the live enemies
    public IReadOnlyList<Enemy> Live => live;

    private float prewarm = 60;

    // Custom methods
    private void E_OnReachedGate(Enemy e)
    {
        e.gameObject.SetActive(false);
        Release(e);
    }

    private Enemy Make()
    {
        Enemy e = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
        e.gameObject.SetActive(false);
        e.OnReachedGate += E_OnReachedGate;
        e.OnDied += E_OnReachedGate;
        return e;
    }

    public Enemy Get()
    {
        Enemy e = free.Count > 0 ? free.Pop() : Make();
        live.Add(e);
        return e;
    }

    private void Release(Enemy e)
    {
        live.Remove(e);
        free.Push(e);
    }

    // Unity lifecycle

    private void Awake()
    {
        free = new Stack<Enemy>();
        live = new List<Enemy>();
        for(int i = 0; i < prewarm; i++)
        {
            free.Push(Make());
        }
    }
}
