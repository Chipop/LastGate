using System;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public int MaxHp { get; private set; } = 1000;
    public float Hp { get; private set; }

    // Events
    public struct OnHpChangedEventArgs {
        public float hp;
        public int maxHp;
    }
    public event Action<OnHpChangedEventArgs> OnHpChanged;
    public event Action OnDestroyed;

    public void Watch(Enemy e)
    {
        // Because we attach this event OnSpawn 
        // Some enemies can have 2 or more events attached to them,
        // which led to double/triple/.. damage in the same attack
        // So we do -= before += so that we always only have 1 event attached to them
        e.OnAttack -= Hit;
        e.OnAttack += Hit;
    }

    private void Hit(Enemy.AttackParams obj)
    {
        if (Hp <= 0) return;

        Hp = Mathf.Max(0, Hp - obj.attackDamage);

        OnHpChanged?.Invoke(
            new OnHpChangedEventArgs {
                hp = Hp,
                maxHp = MaxHp
            }
        );

        //Debug.Log($"Gate {Hp}/{MaxHp}");
        Analytics.Track("gate_damaged", new() { ["hpAfter"] = Hp });
        if (Hp == 0) OnDestroyed?.Invoke();
    }

    private void Heal(int points)
    {
        Hp = Mathf.Min(MaxHp, Hp + points);
        OnHpChanged?.Invoke(
            new OnHpChangedEventArgs
            {
                hp = Hp,
                maxHp = MaxHp
            }
        );
    }

    private void IncreaseMaxHealth(int points)
    {
        MaxHp += points;
        Heal(points);
    }

    private void Awake()
    {
        Hp = MaxHp;
    }
}
