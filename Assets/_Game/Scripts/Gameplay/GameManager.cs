using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Title, Running, LevelUp, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button muteButton;
    [SerializeField] Button quitToTitleButton;
    private float desiredSpeed = 1f;
    private GameState state = GameState.Title;
    public bool IsGameOver => state == GameState.GameOver;

    [SerializeField] GameObject titlePanel;
    [SerializeField] TMP_Text bestScoreTMP;

    [SerializeField] private GameOverUI gameOverUI;

    [SerializeField] Gate gate;
    [SerializeField] EnemySpawner spawner;
    [SerializeField] BonusService bonusService;
    [SerializeField] LevelUpUI levelUpUI;
    [SerializeField] HudUI hudUI;
    [SerializeField] private AudioService audioService;
    private const int RerollBaseCost = 10;
    private const int RerollStep = 5;
    private List<BonusCard> drawedCards;
    private int pendingLevelUps;
    private int freeRerolls;
    private int paidRerolls;
    private readonly RunStats stats = new();
    private Coroutine hitStop;

    // 10 for the first paid reroll, then +5 for each further one in the same panel
    private int RerollCost => RerollBaseCost + RerollStep * paidRerolls;

    
    private void setState(GameState newState)
    {
        state = newState;
        ApplyTimeScale();
        titlePanel.SetActive(state == GameState.Title);
        pausePanel.SetActive(state == GameState.Paused);
        Debug.Log($"Game State: {state}");
    }

    private void ApplyTimeScale()
    {
        switch (state)
        {
            case GameState.Title:
                Time.timeScale = 0f;
                break;
            case GameState.Running:
                Time.timeScale = desiredSpeed;
                break;
            case GameState.LevelUp:
                Time.timeScale = 0f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }

    // Custom Methods
    // Freeze the action for a few frames (fireball blast, brute kill). Only while Running: never freeze a menu.
    public void HitStop(float seconds)
    {
        if (state != GameState.Running) return;
        if (hitStop != null) StopCoroutine(hitStop);
        hitStop = StartCoroutine(HitStopRoutine(seconds));
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);   // real time: scaled time is stopped
        hitStop = null;
        ApplyTimeScale();                                   // back to whatever the CURRENT state wants: x1, x2, or still paused
    }

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
        setState(GameState.LevelUp);
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
            setState(GameState.Running);
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

        stats.NotifyChanged();

        if (pendingLevelUps > 0 && state == GameState.Running)
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

    private void Pause()
    {
        if(state != GameState.Running) return;
        setState(GameState.Paused);
    }

    private void Resume()
    {
        if (state != GameState.Paused) return;
        setState(GameState.Running);
    }

    private void ToggleSpeed()
    {
        desiredSpeed = desiredSpeed == 1f ? 2f : 1f;
        ApplyTimeScale();
        hudUI.SetSpeedLabel(Time.timeScale);
    }

    private void GameOver()
    {
        setState(GameState.GameOver);
        int score = stats.Score;
        int best = PlayerPrefs.GetInt("best_score", 0);
        bool isNewBest = score > best;
        if (isNewBest)
        {
            PlayerPrefs.SetInt("best_score", score);
            PlayerPrefs.Save();
        }
        Analytics.Track("run_end", new() { ["score"] = score, ["wavesCleared"] = stats.wavesCleared });
        
        gameOverUI.Show(score, Mathf.Max(best, score), isNewBest, stats.playerLevel, stats.wavesCleared);
    }

    private void RestartRun()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        audioService.StartMusic();
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
            stats.NotifyChanged();
            paidRerolls++;
        }
        else
        {
            return; //  disnot free, not enough gems = buttonabled
        }

        Analytics.Track("reroll", new() { ["gemsAfter"] = stats.gems });
        ShowCards();
    }

    private void Gate_OnHpChanged(Gate.OnHpChangedEventArgs args)
    {
        hudUI.SetGateHp(args.hp, args.maxHp);
    }

    private void Spawner_OnWaveStarted(EnemySpawner.OnWaveStartedArgs args)
    {
        hudUI.SetLevelWave(args.level, args.wave);
    }
    private void Stats_OnChanged(RunStats.ChangedEventArgs args)
    {
        hudUI.SetXp(args.xp, args.xpNext);
        hudUI.SetGems(args.gems);
    }

    private void RefreshMuteLabel()
    {
        muteButton.GetComponentInChildren<TMP_Text>().text = audioService.IsMuted ? "SOUND : OFF" : "SOUND : ON";
    }

    
    private void StartRun()
    {
        setState(GameState.Running);
        Analytics.Track("run_start");

        spawner.Begin();
    }


    // Unity Lifecycle

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

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

        gate.OnHpChanged += Gate_OnHpChanged;
        spawner.OnWaveStarted += Spawner_OnWaveStarted;
        hudUI.SetGateHp(gate.Hp, gate.MaxHp);
        hudUI.SetLevelWave(1, 1);

        stats.OnChanged += Stats_OnChanged;

        gameOverUI.OnPlayAgainPressed += RestartRun;
        gameOverUI.OnTitlePressed += RestartRun;
        hudUI.OnPausePressed += Pause;
        hudUI.OnSpeedPressed += ToggleSpeed;
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(RestartRun);
        quitToTitleButton.onClick.AddListener(RestartRun);
        muteButton.onClick.AddListener(() => { audioService.ToggleMute(); RefreshMuteLabel(); });

        bestScoreTMP.text = $"BEST {PlayerPrefs.GetInt("best_score", 0)}";
    }

    private void Update()
    {
        if (state != GameState.Title) return;
        if(Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            StartRun();
        }
    }

}
