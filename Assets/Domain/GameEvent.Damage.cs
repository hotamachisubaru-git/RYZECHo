using System;

namespace RYZECHo;

/// <summary>
/// ダメージイベント。
/// </summary>
internal sealed class DamageEvent : GameEvent
{
    public string AttackerName { get; }
    public string VictimName { get; }
    public float DamageAmount { get; }
    public WeaponType Weapon { get; }
    public bool IsHeadshot { get; }

    public DamageEvent(string attackerName, string victimName, float damageAmount, WeaponType weapon, bool isHeadshot)
    {
        AttackerName = attackerName;
        VictimName = victimName;
        DamageAmount = damageAmount;
        Weapon = weapon;
        IsHeadshot = isHeadshot;
    }
}
