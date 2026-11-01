using UnityEngine;

public enum RarityEnum
{
    Common, Rare, Epic
}
public enum EffectEnum
{
    Damage, FireRate, SplitShot, Pierce, Reinforce, Bomb, Frost, Repair
}

[CreateAssetMenu(fileName = "BonusCard", menuName = "Scriptable Objects/BonusCard")]
public class BonusCard : ScriptableObject
{
    public string id;
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;
    public RarityEnum rarity;
    public int maxStack = 5;
    public EffectEnum effect;

}
