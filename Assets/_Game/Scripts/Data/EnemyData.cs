using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string id = "walker";
    public float hp = 30f;
    public float speed  = 1.2f;
    public float size = 0.9f;
    public int gateDamage = 50;
    public int xp = 10;
    public int gemsMin = 0;
    public int gemsMax = 0;
    public Color tint = Color.green;
}
