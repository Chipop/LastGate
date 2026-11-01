using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BonusService : MonoBehaviour
{
    [SerializeField] MageController mage;
    [SerializeField] Gate gate;
    [SerializeField] private List<BonusCard> allCards;
    private HashSet<BonusCard> cardsSeen = new HashSet<BonusCard>();

    // Custom Methods
    public List<BonusCard> Draw3Cards()
    {
        List<BonusCard> availableCards = new List<BonusCard>(allCards);
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
            case EffectEnum.Damage: mage.Damage *= 1.25f; break;
            case EffectEnum.FireRate: mage.FireRate *= 1.20f; break;
            case EffectEnum.SplitShot: mage.Projectiles += 1; mage.Damage *= 0.9f; break;
            case EffectEnum.Pierce: mage.Pierce += 1; break;
            case EffectEnum.Reinforce: gate.IncreaseMaxHealth(300); break;
            case EffectEnum.Repair: gate.Heal(Mathf.RoundToInt(gate.MaxHp * 0.4f)); break;
            case EffectEnum.Bomb: mage.BombLevel++; break;
            case EffectEnum.Frost: mage.FrostLevel++; break;
        }
    }
}