using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private TMP_Text bestScoreTMP;
    [SerializeField] private GameObject newBestTag;
    [SerializeField] private TMP_Text detailsTextTMP;
    [SerializeField] private TMP_Text wavesClearedTMP;
    [SerializeField] private TMP_Text leaderboardStatusTMP;
    [SerializeField] private TMP_Text leaderboardFooterTMP;
    [SerializeField] private RectTransform leaderboardRowTemplate;
    [SerializeField] private GameObject leaderboardColumns;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button titleButton;
    private readonly List<RectTransform> leaderboardRows = new();
    private Coroutine leaderboardRequest;

    public event Action OnPlayAgainPressed;
    public event Action OnTitlePressed;

    public void Show(int score, int bestScore, bool isNewBest, int levelsCleared, int wavesCleared)
    {
        scoreTMP.text = $"{score:N0}";
        bestScoreTMP.text = $"BEST {bestScore:N0}";
        newBestTag.SetActive(isNewBest);
        string levelLabel = levelsCleared == 1 ? "LEVEL" : "LEVELS";
        string waveLabel = wavesCleared == 1 ? "WAVE" : "WAVES";
        detailsTextTMP.alignment = TextAlignmentOptions.Center;
        detailsTextTMP.text = $"<color=#FFCC66>{levelsCleared}</color> {levelLabel} CLEARED";
        wavesClearedTMP.text = $"<color=#FFCC66>{wavesCleared}</color> {waveLabel} CLEARED";
        gameObject.SetActive(true);
        if (leaderboardRequest != null) StopCoroutine(leaderboardRequest);
        HideLeaderboardRows();
        leaderboardRequest = StartCoroutine(ShowLeaderboard());
    }

    private IEnumerator ShowLeaderboard()
    {
        leaderboardColumns.SetActive(false);
        leaderboardFooterTMP.text = "";
        leaderboardStatusTMP.text = "Loading scores...";
        DjangoPlaytest.LeaderboardResult result = null;
        yield return DjangoPlaytest.FetchLeaderboard(value => result = value);
        if (result == null)
        {
            leaderboardStatusTMP.text = "Scores unavailable";
            yield break;
        }

        if (result.entries.Length == 0)
        {
            leaderboardStatusTMP.text = "No scores yet. Be the first!";
            yield break;
        }

        leaderboardStatusTMP.text = "";
        leaderboardColumns.SetActive(true);
        int playerRank = 0;
        int visibleRows = Mathf.Min(result.entries.Length, 10);
        for (int index = 0; index < visibleRows; index++)
        {
            var entry = result.entries[index];
            if (index == leaderboardRows.Count)
                leaderboardRows.Add(Instantiate(leaderboardRowTemplate, leaderboardRowTemplate.parent));
            RectTransform row = leaderboardRows[index];
            // Use the Inspector row height as the distance between rows.
            row.anchoredPosition = leaderboardRowTemplate.anchoredPosition
                + Vector2.down * (index * leaderboardRowTemplate.rect.height);
            TMP_Text[] cells = row.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text cell in cells) cell.color = MenuTheme.Cream;
            cells[0].text = $"{index + 1:00}";
            cells[0].color = index switch
            {
                0 => MenuTheme.Gold,
                1 => MenuTheme.Steel,
                2 => new Color32(224, 151, 99, 255),
                _ => cells[0].color
            };
            cells[1].text = entry.isYou ? "You" : entry.name;
            cells[2].text = $"{entry.score:N0}";
            row.GetComponent<Image>().color = entry.isYou
                ? MenuTheme.Selected
                : index % 2 == 0
                    ? MenuTheme.Navy
                    : MenuTheme.AlternateNavy;
            if (entry.isYou)
            {
                playerRank = index + 1;
                foreach (TMP_Text cell in cells) cell.color = MenuTheme.Gold;
            }
            row.gameObject.SetActive(true);
        }
        if (playerRank > 0) leaderboardFooterTMP.text = $"YOUR RANK: #{playerRank}";
        else if (result.personalBest >= 0)
            leaderboardFooterTMP.text = $"YOUR ONLINE BEST: {result.personalBest:N0}";
        else if (!Analytics.HasConsent) leaderboardFooterTMP.text = "Score sharing is off";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        leaderboardRequest = null;
        HideLeaderboardRows();
    }

    private void HideLeaderboardRows()
    {
        foreach (RectTransform row in leaderboardRows)
        {
            if (row != null) row.gameObject.SetActive(false);
        }
    }
    
    private void Awake()
    {
        leaderboardFooterTMP.fontSize = MenuTheme.Caption;
        playAgainButton.onClick.AddListener(() => OnPlayAgainPressed?.Invoke());
        titleButton.onClick.AddListener(() => OnTitlePressed?.Invoke());
    }
}
