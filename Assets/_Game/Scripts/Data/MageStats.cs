using UnityEngine;

[CreateAssetMenu(fileName = "MageStats", menuName = "Scriptable Objects/MageStats")]
public class MageStats : ScriptableObject
{
    public float damage = 10f;
    // Shots per second
    public float fireRate = 2f;
    public float projectileSpeed = 8f;
    // number of Projectiles in the same attack
    public int projectiles = 1;
    // How many enemies I pierce with the same projectile
    public int pierce = 0;
}
