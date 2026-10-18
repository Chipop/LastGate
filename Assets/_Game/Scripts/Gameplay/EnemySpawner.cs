using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private LevelData level1;
    [SerializeField] private Gate gate;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private EnemyData walker;
    [SerializeField] private EnemyData runner;
    [SerializeField] private EnemyData brute;
    [SerializeField] private float waveInterval = 15f;
    [SerializeField] private float spawnY = 7f;
    [SerializeField] private float gateLineY = -7f;
    [SerializeField] private float spawnXMin = -4.8f;
    [SerializeField] private float spawnXMax = 4.8f;
    [SerializeField] private int waveAliveThreshold = 3;
    [SerializeField] private float waveTimeout = 25f;
    [SerializeField] private float restBetweenLevels = 3f;

    private int level = 1;

    // Custom methods
    public void Spawn(EnemyData enemyData, int level)
    {
        float randomX = Random.Range(spawnXMin, spawnXMax);
        Vector2 spawnPos = new Vector2(randomX, spawnY);

        // hp multiplier depending on the level
        float hp = enemyData.hp * (1f + 0.25f * (level-1));
        float speed = enemyData.speed * (Mathf.Min(1.5f, 1f + 0.03f * (level - 1)));

        Enemy enemy = enemyPool.Get();
        enemy.Init(spawnPos, hp, speed, enemyData.attackDamage, enemyData.attackInterval, enemyData.xp, enemyData.size, enemyData.tint, gateLineY);

        gate.Watch(enemy);
    }

    private IEnumerator SpawnGroupRoutine(SpawnGroup group)
    {
        yield return new WaitForSeconds(group.startsAfter);
        for (int i = 0; i < group.count; i++)
        {
            Spawn(group.enemy, 1);
            yield return new WaitForSeconds(group.interval);
        }
    }

    private IEnumerator RunWaveRoutine(List<SpawnGroup> spawnGroups)
    {
        List<Coroutine> corroutines = new List<Coroutine>();

        foreach (var group in spawnGroups)
        {
            corroutines.Add(StartCoroutine(SpawnGroupRoutine(group)));
        }

        foreach (var courroutineHandle in corroutines)
        {
            yield return courroutineHandle;
        }

        Debug.Log("wave spawned");
        float waveTimer = 0;
        while (enemyPool.Live.Count >= waveAliveThreshold && waveTimer < waveTimeout)
        {
            waveTimer += Time.deltaTime;
            yield return null;
        }
        Debug.Log("wave cleared");
    }

    private IEnumerator RunLevel1(){

        foreach (var wave in level1.waves)
        {
            yield return RunWaveRoutine(wave.groups);
        }
        while (enemyPool.Live.Count > 0)
        {
            yield return null;
        }
        yield return new WaitForSeconds(restBetweenLevels);
    }

    //Unity Life cycle
    private void Start()
    {
        StartCoroutine(RunLevel1());
        Debug.Log("Fun");
    }
}
