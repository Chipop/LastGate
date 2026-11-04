using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    [SerializeField] private AudioService audioService;
    [SerializeField] private MageController mage;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private GateFeedback gateFeedback;
    [SerializeField] private LevelUpUI levelUpUI;
    [SerializeField] private AudioClip shoot;
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip gateHit;
    [SerializeField] private AudioClip levelUp;
    [SerializeField] private AudioClip pick;

    // Custom Methods
    private void Mage_OnFired() { audioService.PlaySfx(shoot, 0.6f); }
    private void Spawner_OnEnemyDamaged(Enemy enemy) { audioService.PlaySfx(hit, 0.4f); }
    private void Spawner_OnEnemyKilled(Enemy enemy) { audioService.PlaySfx(enemy.Data.deathSound); }
    private void GateFeedback_OnFeedbackTriggered() { audioService.PlaySfx(gateHit); }
    private void LevelUpUI_OnShown() { audioService.PlaySfx(levelUp); }
    private void LevelUpUI_OnPicked(BonusCard card) { audioService.PlaySfx(pick); }

    // Unity Lifecycle
    private void Start()
    {
        mage.OnFired += Mage_OnFired;
        spawner.OnEnemyDamaged += Spawner_OnEnemyDamaged;
        spawner.OnEnemyKilled += Spawner_OnEnemyKilled;
        gateFeedback.OnFeedbackTriggered += GateFeedback_OnFeedbackTriggered;
        levelUpUI.OnShown += LevelUpUI_OnShown;
        levelUpUI.OnPicked += LevelUpUI_OnPicked;
    }
}
