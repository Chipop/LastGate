using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private Image[] cardImages;
    [SerializeField] TMP_Text[] names;
    [SerializeField] TMP_Text[] descriptions;
    [SerializeField] TMP_Text[] stacks;
    [SerializeField] private Image[] icons;
    [SerializeField] private GameObject[] newTags;
    [SerializeField] private Button rerollButton;
    [SerializeField] TMP_Text rerollLabel;
    [SerializeField] Color[] rarityColors;       // size 3 in the Inspector: Common, Rare, Epic
    private List<BonusCard> currentBonusCards = new List<BonusCard>();

    public event Action<BonusCard> OnPicked;
    public event Action OnReroll;

    // Custom Methods
    public void Show(List<BonusCard> cards, BonusService bonusService, int gems, int rerollCost, bool rerollFree)
    {
        gameObject.SetActive(true);
        currentBonusCards = new List<BonusCard>(cards);

        // Loop over the BUTTONS, not over the cards: the unused buttons must be hidden
        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool hasCard = i < cards.Count;
            cardButtons[i].gameObject.SetActive(hasCard);
            if (!hasCard) continue;

            BonusCard card = cards[i];
            names[i].text = card.cardName;
            descriptions[i].text = bonusService.Describe(card);
            stacks[i].text = card.maxStack == 0 ? "" : $"Stack {bonusService.StackOf(card) + 1}/{card.maxStack}";
            cardImages[i].color = rarityColors[(int)card.rarity];
            if (card.icon != null) icons[i].sprite = card.icon;
            newTags[i].GetComponent<TMP_Text>().enabled = bonusService.IsNew(card);   // hide the text, keep the layout slot
        }

        rerollLabel.text = rerollFree ? "Reroll - FREE" : $"Reroll - {rerollCost} gems";
        rerollButton.interactable = rerollFree || gems >= rerollCost;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Unity Lifecycle
    private void Awake()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;                       // copy: each lambda needs its own index
            cardButtons[i].onClick.AddListener(() => OnPicked?.Invoke(currentBonusCards[index]));
        }
        rerollButton.onClick.AddListener(() => OnReroll?.Invoke());
    }
}