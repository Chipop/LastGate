using System;
using UnityEngine;

public class RunStats
{
    private GameConfig config;
    public int xp;
    public int totalXp;
    public int playerLevel = 1;
    public int gems;
    public int kills;
    public int wavesCleared;
    public int LevelsCleared;

    public int XpNext => Mathf.RoundToInt(config.xpBase * Mathf.Pow(config.xpGrowth, playerLevel - 1));
    public int Score => totalXp + 100 * wavesCleared + 500 * LevelsCleared;

    public RunStats(GameConfig config)
    {
        this.config = config;
    }

    public struct ChangedEventArgs
    {
        public int xp;
        public int xpNext;
        public int playerLevel;
        public int gems;
        public int score;
    }

    public event Action<ChangedEventArgs> OnChanged;

    public void NotifyChanged()
    {
        OnChanged?.Invoke(new ChangedEventArgs
        {
            xp = xp,
            xpNext = XpNext,
            playerLevel = playerLevel,
            gems = gems,
            score = Score
        });
    }

}
