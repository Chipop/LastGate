using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private List<LevelData> authoredLevels;
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

    public int Level { get; private set; } = 1;
    public int Wave {  get; private set; }

    public struct OnWaveStartedArgs {
        public int level;
        public int wave;
    }
    public event Action<OnWaveStartedArgs> OnWaveStarted;
    public event Action OnLevelCleared;
    public event Action OnWaveCleared;
    public event Action<Enemy> OnEnemyKilled;
    public event Action<Enemy> OnEnemyDamaged;
    private void Killed(Enemy e) => OnEnemyKilled?.Invoke(e);

    private void Enemy_OnDamaged(Enemy enemy) 
    {
        OnEnemyDamaged?.Invoke(enemy); 
    }
    
    // Custom methods
    public void Spawn(EnemyData enemyData, int level)
    {
        float randomX = UnityEngine.Random.Range(spawnXMin, spawnXMax);
        Vector2 spawnPos = new Vector2(randomX, spawnY);

        // hp multiplier depending on the level
        float hp = enemyData.hp * (1f + config.hpGrowthPerLevel * (level-1));
        float speed = enemyData.speed * Mathf.Min(config.speedMultiplierCap, 1f + config.speedGrowthPerLevel * (level - 1));

        Enemy enemy = enemyPool.Get();
        enemy.Init(enemyData, spawnPos, hp, speed, enemyData.attackDamage, enemyData.attackInterval, enemyData.xp, enemyData.size, enemyData.tint, gateLineY);
        gate.Watch(enemy);
        enemy.OnDied -= Killed;
        enemy.OnDied += Killed;
        enemy.OnDamaged -= Enemy_OnDamaged;
        enemy.OnDamaged += Enemy_OnDamaged;
    }
    

    private IEnumerator SpawnGroupRoutine(SpawnGroup group)
    {
        yield return new WaitForSeconds(group.startsAfter);
        for (int i = 0; i < group.count; i++)
        {
            Spawn(group.enemy, Level);
            yield return new WaitForSeconds(group.interval);
        }
    }

    private IEnumerator RunWaveRoutine(List<SpawnGroup> spawnGroups)
    {
        List<Coroutine> corroutines = new List<Coroutine>();

        OnWaveStarted?.Invoke(
            new OnWaveStartedArgs
            {
                level = Level,
                wave = Wave
            }
        );

        foreach (var group in spawnGroups)
        {
            corroutines.Add(StartCoroutine(SpawnGroupRoutine(group)));
        }

        foreach (var courroutineHandle in corroutines)
        {
            yield return courroutineHandle;
        }

        float waveTimer = 0;
        while (enemyPool.Live.Count >= config.waveAliveThreshold && waveTimer < config.waveTimeout)
        {
            waveTimer += Time.deltaTime;
            yield return null;
        }

        OnWaveCleared?.Invoke();
    }

    private IEnumerator RunLevels()
    {
        while (true)
        {
            List<WaveData> waves = Level <= authoredLevels.Count
                ? authoredLevels[Level - 1].waves
                : EndlessGenerator.Build(Level, walker, runner, brute);

            Wave = 0;
            yield return RunWaves(waves);
            Level++;
        }
    }

    private IEnumerator RunWaves(List<WaveData> waves)
    {
        foreach (var wave in waves)
        {
            Wave++;
            yield return RunWaveRoutine(wave.groups);
        }
        while (enemyPool.Live.Count > 0)
        {
            yield return null;
        }
        OnLevelCleared?.Invoke();
        yield return new WaitForSeconds(config.restBetweenLevels);
    }

    public void Begin()
    {
        StartCoroutine(RunLevels());
    }

}
