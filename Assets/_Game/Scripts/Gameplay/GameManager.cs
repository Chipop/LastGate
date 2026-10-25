using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Gate gate;
    [SerializeField] EnemySpawner spawner;
    [SerializeField] BonusService bonusService;
    [SerializeField] LevelUpUI levelUpUI;
    private const int RerollBaseCost = 10;
    private const int RerollStep = 5;
    private List<BonusCard> drawedCards;
    private int pendingLevelUps;
    private int freeRerolls;
    private int paidRerolls;
    private bool choosing;
    private readonly RunStats stats = new();

    public bool IsGameOver { get; private set; }
    // 10 for the first paid reroll, then +5 for each further one in the same panel
    private int RerollCost => RerollBaseCost + RerollStep * paidRerolls;

    // Custom Methods
    private void ShowCards()
    {
        drawedCards = bonusService.Draw3Cards();
        levelUpUI.Show(drawedCards, bonusService, stats.gems, RerollCost, freeRerolls > 0);
        // After Show: the view has already asked IsNew, only now do the cards count as seen
        bonusService.MarkSeen(drawedCards);
        Analytics.Track("bonus_offered", new() { ["cards"] = string.Join(",", drawedCards.ConvertAll(card => card.id)) });
    }

    private void OpenLevelUp()
    {
        choosing = true;
        Time.timeScale = 0f;
        freeRerolls = 1; // one free reroll per panel
        paidRerolls = 0;
        ShowCards();
    }

    private void CloseLevelUp()
    {
        pendingLevelUps--;
        if (pendingLevelUps > 0)
        {
            OpenLevelUp();
        }
        else
        {
            Time.timeScale = 1f;
            choosing = false;
            levelUpUI.Hide();
        }
    }

    private void OnKill(Enemy enemy)
    {
        int xpGained = Mathf.RoundToInt(enemy.Xp);
        stats.xp += xpGained;
        stats.totalXp += xpGained;
        stats.kills++;
        stats.gems += Random.Range(enemy.Data.gemsMin, enemy.Data.gemsMax + 1);   // int Range excludes the max
        Debug.Log($"Xp +{xpGained}  total {stats.xp}  gems {stats.gems}");

        while (stats.xp >= stats.XpNext)
        {
            stats.xp -= stats.XpNext;
            stats.playerLevel++;
            pendingLevelUps++;
            Analytics.Track("level_up", new() { ["playerLevel"] = stats.playerLevel });
            Debug.Log("Level Up : " + stats.playerLevel);
        }

        if (pendingLevelUps > 0 && !choosing)
        {
            OpenLevelUp();
        }
    }

    private void OnWaveCleared()
    {
        stats.wavesCleared++;
    }

    private void OnLevelCleared()
    {
        stats.LevelsCleared++;
    }

    private void GameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        Analytics.Track("run_end", new() { ["score"] = stats.totalXp, ["wavesCleared"] = stats.wavesCleared });
        Debug.Log("Game Over");
    }

    // Handlers
    private void LevelUpUI_OnPicked(BonusCard card)
    {
        bonusService.ApplyBonusCard(card);
        Analytics.Track("bonus_picked", new() { ["card"] = card.id });
        CloseLevelUp();
    }

    private void LevelUpUI_OnReroll()
    {
        if (freeRerolls > 0)
        {
            freeRerolls--;
        }
        else if (stats.gems >= RerollCost)
        {
            stats.gems -= RerollCost;
            paidRerolls++;
        }
        else
        {
            return; // not free, not enough gems = button disabled
        }

        Analytics.Track("reroll", new() { ["gemsAfter"] = stats.gems });
        ShowCards();
    }

    // Unity Lifecycle
    void Start()
    {
        gate.OnDestroyed += GameOver;
        spawner.OnWaveStarted += args => Debug.Log($"Level {args.level} wave {args.wave}");
        spawner.OnLevelCleared += OnLevelCleared;
        spawner.OnWaveCleared += OnWaveCleared;
        spawner.OnEnemyKilled += OnKill;
        levelUpUI.OnPicked += LevelUpUI_OnPicked;
        levelUpUI.OnReroll += LevelUpUI_OnReroll;
        levelUpUI.Hide();

        Analytics.Track("run_start");
        spawner.Begin();
    }
}