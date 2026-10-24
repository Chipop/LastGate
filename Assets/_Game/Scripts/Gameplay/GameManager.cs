using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Gate gate;
    [SerializeField] EnemySpawner spawner;

    public bool IsGameOver {  get; private set; }

    private void GameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("Game Over");
    }


    void Start()
    {
        Analytics.Track("run_start");
        gate.OnDestroyed += GameOver;
        spawner.Begin();
        spawner.OnWaveStarted += args => Debug.Log($"Level {args.level} wave {args.wave}");
        spawner.OnEnemyKilled += enemy => Debug.Log($"Killed enemy: {enemy.Id}");
    }
}
