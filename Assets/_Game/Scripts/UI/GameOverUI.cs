using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private TMP_Text bestScoreTMP;
    [SerializeField] private GameObject newBestTag;
    [SerializeField] private TMP_Text detailsTextTMP;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button titleButton;

    public event Action OnPlayAgainPressed;
    public event Action OnTitlePressed;

    public void Show(int score, int bestScore, bool isNewBest, int level, int waves)
    {
        scoreTMP.text = $"SCORE {score}";
        bestScoreTMP.text = $"BEST {bestScore}";
        newBestTag.SetActive(isNewBest);
        detailsTextTMP.text = $"Level {level} - {waves} waves cleared";
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    private void Awake()
    {
        playAgainButton.onClick.AddListener(() => OnPlayAgainPressed?.Invoke());
        titleButton.onClick.AddListener(() => OnTitlePressed?.Invoke());
    }
}
