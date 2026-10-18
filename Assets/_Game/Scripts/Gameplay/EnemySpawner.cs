using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Gate gate;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float spawnY = 7f;
    [SerializeField] private float gateLineY = -7f;
    [SerializeField] private float spawnXMin = -4.8f;
    [SerializeField] private float spawnXMax = 4.8f;
    private int level = 1;

    // Custom methods
    public void Spawn(EnemyData d, int level)
    {
        float randomX = Random.Range(spawnXMin, spawnXMax);
        Vector2 spawnPos = new Vector2(randomX, spawnY);

        // hp multiplier depending on the level
        float hp = enemyData.hp * (1f + 0.25f * (level-1));
        float speed = d.speed * (Mathf.Min(1.5f, 1f + 0.03f * (level - 1)));

        Enemy enemy = enemyPool.Get();
        enemy.Init(spawnPos, hp, speed, enemyData.attackDamage, enemyData.attackInterval, enemyData.xp, enemyData.size, enemyData.tint, gateLineY);

        gate.Watch(enemy);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            Spawn(enemyData, level);
            // Pause this method and hand control back to Unity for X seconds
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    //Unity Life cycle
    private void Start()
    {
        // Start the coroutine when the game begins
        StartCoroutine(SpawnRoutine());
    }
}
