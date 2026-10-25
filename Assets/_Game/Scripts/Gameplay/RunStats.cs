using UnityEngine;

public class RunStats
{
    public int xp;
    public int totalXp;
    public int playerLevel = 1;
    public int gems;
    public int kills;
    public int wavesCleared;
    public int LevelsCleared;

    public int XpNext => Mathf.RoundToInt(50f * Mathf.Pow(1.35f, playerLevel - 1));

}
