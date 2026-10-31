using System.Collections.Generic;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float sameHeightTolerance = 0.5f;
    // Reused between calls: no garbage per shot. The caller must not keep it.
    private List<Enemy> targetsBuffer = new List<Enemy>();

    private bool IsBetterTarget(Enemy candidate, Enemy current)
    {
        if (current == null) return true;

        float heightDifference = candidate.transform.position.y - current.transform.position.y;
        if (Mathf.Abs(heightDifference) > sameHeightTolerance)
        {
            // lower = closer to the gate
            return heightDifference < 0f;
        }

        if (!Mathf.Approximately(candidate.Hp, current.Hp))
        {
            // finish the weakest first
            return candidate.Hp < current.Hp;
        }

        return SqrDistanceToOrigin(candidate) < SqrDistanceToOrigin(current);
    }

    private float SqrDistanceToOrigin(Enemy enemy)
    {
        return ((Vector2)enemy.transform.position - (Vector2)muzzle.position).sqrMagnitude;
    }

    public Enemy GetBestTarget()
    {
        List<Enemy> targets = GetBestTargets(1);
        return targets.Count > 0 ? targets[0] : null;
    }

    // The best 'count' enemies, best first. Fewer if fewer are alive.
    public List<Enemy> GetBestTargets(int count)
    {
        targetsBuffer.Clear();
        for (int slot = 0; slot < count; slot++)
        {
            Enemy best = null;
            foreach (Enemy enemy in enemyPool.Live)
            {
                // already picked for an earlier slot
                if (targetsBuffer.Contains(enemy)) continue;
                if (IsBetterTarget(enemy, best)) best = enemy;
            }
            if (best == null) break;
            targetsBuffer.Add(best);
        }
        return targetsBuffer;
    }
}
