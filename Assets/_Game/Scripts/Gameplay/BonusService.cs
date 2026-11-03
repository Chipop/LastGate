using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BonusService : MonoBehaviour
{
    [SerializeField] MageController mage;
    [SerializeField] Gate gate;
    [SerializeField] private BombLauncher bombs;
    [SerializeField] private List<BonusCard> allCards;
    private const float DamageMult = 1.25f;
    private const float FireRateMult = 1.20f;
    private const float SplitDamageMult = 0.9f;
    private const int ReinforceHp = 300;
    private const float RepairFraction = 0.4f;
    private HashSet<BonusCard> cardsSeen = new HashSet<BonusCard>();
    private Dictionary<BonusCard, int> stacks = new Dictionary<BonusCard, int>();

    // Custom Methods
    public int StackOf(BonusCard card) => stacks.TryGetValue(card, out int n) ? n : 0;

    // maxStack 0 = unlimited (Repair)
    private bool CanStillPick(BonusCard card) => card.maxStack == 0 || StackOf(card) < card.maxStack;

    // Two lines: the rule (typed on the asset) and what you will actually have after this pick, from the live numbers.
    // One case per effect, same order as ApplyBonusCard.
    public string Describe(BonusCard card)
    {
        string rule = card.description;
        string result;
        switch (card.effect)
        {
            case EffectEnum.Damage:    return rule;                                   // the % says it all
            case EffectEnum.FireRate:  return rule;
            case EffectEnum.SplitShot: result = $"{mage.Projectiles} -> {mage.Projectiles + 1}"; break;
            case EffectEnum.Pierce:    result = $"{mage.Pierce + 1} -> {mage.Pierce + 2}"; break;
            case EffectEnum.Reinforce: result = $"Gate max HP becomes {gate.MaxHp + ReinforceHp}"; break;
            case EffectEnum.Repair:    result = $"Heals {Mathf.RoundToInt(gate.MaxHp * RepairFraction)} HP now"; break;
            case EffectEnum.Bomb:
            {
                int next = mage.BombLevel + 1;
                if (mage.BombLevel > 0) rule = "Bigger, stronger fireball";
                result = $"{bombs.DamageAt(next):0} damage, {bombs.RadiusAt(next) * 2f:0} tiles wide";
                break;
            }
            case EffectEnum.Frost:
            {
                return $"Enemies {mage.FrostSlowAt(mage.FrostLevel + 1) * 100f:0} % slower for {mage.FrostSeconds:0} s";   // the number is the rule
            }
            default: return rule;
        }
        return rule + "\n" + result;
    }

    public List<BonusCard> Draw3Cards()
    {
        List<BonusCard> availableCards = allCards.Where(CanStillPick).ToList();
        List<BonusCard> pickedCards = new List<BonusCard>();

        for (int i = 0; i < 3 && availableCards.Count > 0; i++)
        {
            BonusCard card = DrawCard(availableCards);
            pickedCards.Add(card);
            availableCards.Remove(card);
        }

        return pickedCards;
    }

    private BonusCard DrawCard(List<BonusCard> cardsList)
    {
        float chance = Random.Range(0f, 100f);
        List<BonusCard> cardsPool = new List<BonusCard>();

        float probTotal = 0f;
        float probCommon = 0f;
        float probRare = 0f;
        float probEpic = 0f;

        probCommon += cardsList.Any(c => c.rarity == RarityEnum.Common) ? 60f : 0f;
        probRare += cardsList.Any(c => c.rarity == RarityEnum.Rare) ? 30f : 0f;
        probEpic += cardsList.Any(c => c.rarity == RarityEnum.Epic) ? 10f : 0f;

        probTotal = probCommon + probRare + probEpic;

        probCommon = probCommon == 0f ? 0f : probCommon / probTotal * 100f;
        probRare = probRare == 0f ? 0f : probRare / probTotal * 100f;
        probEpic = probEpic == 0f ? 0f : probEpic / probTotal * 100f;

        if (probEpic > 0 && chance >= probCommon + probRare)
        {
            cardsPool = cardsList.Where(c => c.rarity == RarityEnum.Epic).ToList();
        }
        else if (probRare > 0 && chance >= probCommon)
        {
            cardsPool = cardsList.Where(c => c.rarity == RarityEnum.Rare).ToList();
        }
        else if (probCommon > 0)
        {
            cardsPool = cardsList.Where(c => c.rarity == RarityEnum.Common).ToList();
        }

        int index = Random.Range(0, cardsPool.Count);
        return cardsPool[index];
    }

    // The service owns the "has the player already seen this card?" state; the view only displays the answer
    public bool IsNew(BonusCard card)
    {
        return !cardsSeen.Contains(card) && card.effect != EffectEnum.Repair;
    }

    public void MarkSeen(List<BonusCard> cards)
    {
        foreach (BonusCard card in cards)
        {
            cardsSeen.Add(card);
        }
    }

    public void ApplyBonusCard(BonusCard card)
    {
        switch (card.effect)
        {
            case EffectEnum.Damage: mage.Damage *= DamageMult; break;
            case EffectEnum.FireRate: mage.FireRate *= FireRateMult; break;
            case EffectEnum.SplitShot: mage.Projectiles += 1; mage.Damage *= SplitDamageMult; break;
            case EffectEnum.Pierce: mage.Pierce += 1; break;
            case EffectEnum.Reinforce: gate.IncreaseMaxHealth(ReinforceHp); break;
            case EffectEnum.Repair: gate.Heal(Mathf.RoundToInt(gate.MaxHp * RepairFraction)); break;
            case EffectEnum.Bomb: mage.BombLevel++; break;
            case EffectEnum.Frost: mage.FrostLevel++; break;
        }
        stacks[card] = StackOf(card) + 1;
    }
}