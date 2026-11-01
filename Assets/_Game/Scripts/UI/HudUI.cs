using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelWaveText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private Slider gateHpBar;
    [SerializeField] private TMP_Text gateHpText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private TMP_Text speedButtonLabel;

    public event Action OnPausePressed;
    public event Action OnSpeedPressed;

    // Custom Methods
    public void SetLevelWave(int level, int wave)
    {
        levelWaveText.text = $"Level {level} - Wave {wave}/3";
    }

    public void SetGems(int gems)
    {
        gemsText.text = $"{gems} Gems";
    }

    public void SetXp(int xp, int xpNext)
    {
        xpBar.value = (float)xp / xpNext;
    }

    public void SetGateHp(float hp, int maxHp)
    {
        gateHpBar.value = hp / maxHp;
        gateHpText.text = $"{Mathf.CeilToInt(hp)} / {maxHp}";
    }

    public void SetSpeedLabel(float speed)
    {
        speedButtonLabel.text = $"x{speed:0}";
    }

    // Unity Lifecycle
    private void Awake()
    {
        pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
        speedButton.onClick.AddListener(() => OnSpeedPressed?.Invoke());
    }
}
