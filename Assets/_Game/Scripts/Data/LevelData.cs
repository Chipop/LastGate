using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SpawnGroup
{
    public EnemyData enemy;
    public int count;
    public float interval;
    public float startsAfter;
}

[Serializable]
public class WaveData
{
    public List<SpawnGroup> groups = new();
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    public int level = 1;
    public List<WaveData> waves = new();
}
