using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
public class GameConfig : ScriptableObject
{
    
    [Header("Gate")]
    public int gateMaxHp = 1000;

    [Header("Enemy scaling per level")]
    public float hpGrowthPerLevel = 0.25f;
    public float speedGrowthPerLevel = 0.03f;
    public float speedMultiplierCap = 1.5f;

    [Header("XP")]
    public float xpBase = 50f;
    public float xpGrowth = 1.35f;

    [Header("Rerolls")]
    public int rerollBaseCost = 10;
    public int rerollStep = 5;

    [Header("Waves")]
    public float restBetweenLevels = 3f;
    public int waveAliveThreshold = 3;
    public float waveTimeout = 25f;
}
