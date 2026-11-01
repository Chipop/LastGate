using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public Sprite[] frames;
    public float hitRadius = 0.5f;
    public Color deathColor = Color.red;

    public string id = "walker";
    public float hp = 30f;
    public float attackDamage = 10f;
    public float attackInterval = 1f;
    public float speed  = 1.2f;
    public float size = 0.9f;
    public int xp = 10;
    public int gemsMin = 0;
    public int gemsMax = 0;
    public Color tint = Color.green;
}
