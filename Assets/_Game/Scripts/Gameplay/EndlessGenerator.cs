using System.Collections.Generic;
using UnityEngine;

public static class EndlessGenerator
{
    public static List<WaveData> Build(int level, EnemyData walker, EnemyData runner, EnemyData brute)
    {
        int k = Mathf.Max(0, level - 5);
        List<WaveData> waveList = new List<WaveData>();

        for(int i = 0; i < 3; i++)
        {
            WaveData wave = new WaveData();
            int waveNumber = i + 1;
            SpawnGroup walkerSpawnGroup = new SpawnGroup
            {
                enemy = walker,
                count = Mathf.RoundToInt(10f + 2.5f * k + 2f * waveNumber),
                interval = 1,
                startsAfter = 0
            };
            SpawnGroup runnerSpawnGroup = new SpawnGroup
            {
                enemy = runner,
                count = Mathf.RoundToInt(6f + 2.5f * k + waveNumber),
                interval = 1,
                startsAfter = 3
            };
            SpawnGroup bruteSpawnGroup = new SpawnGroup
            {
                enemy = brute,
                count = 1 + k / 2 + (waveNumber == 3 ? 1 : 0),
                interval = 1,
                startsAfter = 6
            };
            wave.groups.Add(walkerSpawnGroup);
            wave.groups.Add(runnerSpawnGroup);
            wave.groups.Add(bruteSpawnGroup);
            waveList.Add(wave);
        }

        return waveList;
    }

}
