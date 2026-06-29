using System;
using RYZECHo;

namespace RYZECHo.TacticalProto;

/// <summary>
/// タクティカルシューター用キャラクターモデル（既存のActorを簡略化）
/// HP、Shield、位置、ステータス、死亡判定、シールド再生ロジック
/// </summary>
public sealed class TacticalActor
{
    public required string Name { get; init; }
    public required AgentKind Agent { get; set; }
    public required ActorType Type { get; init; }
    public required WeaponType Weapon { get; set; }
    public WeaponType? PrimaryWeapon { get; set; }
    public required System.Numerics.Vector2 Position { get; set; }
    public required System.Numerics.Vector2 HomePosition { get; init; }
    public required float Radius { get; init; }
    public required float MaxHealth { get; init; }
    public required float MaxShield { get; init; }
    public required float HearingRange { get; init; }
    public required float BaseMoveSpeed { get; init; }

    public float Health { get; set; }
    public float Shield { get; set; }
    public float ShieldRegenDelay { get; set; }
    public float FireCooldown { get; set; }
    public float SkillOneCooldown { get; set; }
    public float SkillTwoCooldown { get; set; }
    public float UltimateCharge { get; set; }
    public float FacingAngle { get; set; }
    public bool IsBoss { get; set; }

    /// <summary>死亡判定: Health > 0.01f</summary>
    public bool IsAlive => Health > 0.01f;

    public TacticalActor()
    {
        Health = 0f;
        Shield = 0f;
        ShieldRegenDelay = 0f;
        FireCooldown = 0f;
    }

    /// <summary>
    /// シールド再生ロジック（ShieldRegenDelay=2.4f, 22%/sec + 8/秒）
    /// </summary>
    public void UpdateShieldRegen(float deltaSeconds)
    {
        if (!IsAlive || MaxShield <= 0f) return;

        ShieldRegenDelay = Math.Max(0f, ShieldRegenDelay - deltaSeconds);
        if (ShieldRegenDelay > 0f || Shield >= MaxShield) return;

        Shield = Math.Min(MaxShield, Shield + (MaxShield * 0.22f * deltaSeconds) + (8f * deltaSeconds));
    }

    /// <summary>ダメージ適用（既存のApplyDamageロジックを流用）</summary>
    public void TakeDamage(float damage, TacticalActor? attacker = null)
    {
        if (!IsAlive || damage <= 0f) return;

        ShieldRegenDelay = 2.4f;

        if (Shield > 0f)
        {
            var absorbed = Math.Min(Shield, damage);
            Shield -= absorbed;
            damage -= absorbed;
        }

        if (damage > 0f)
        {
            Health = Math.Max(0f, Health - damage);
        }
    }

    /// <summary>フル回復（ラウンド開始時等に使用）</summary>
    public void FullRestore()
    {
        Health = MaxHealth;
        Shield = MaxShield;
        ShieldRegenDelay = 0f;
        FireCooldown = 0f;
        Position = HomePosition;
    }

    /// <summary>移動（WASD移動対応）</summary>
    public void Move(TacticalInput input, float deltaSeconds)
    {
        if (!IsAlive) return;

        float dx = 0f, dy = 0f;
        if (input.MoveRight) dx += 1f;
        if (input.MoveLeft) dx -= 1f;
        if (input.MoveDown) dy += 1f;
        if (input.MoveUp) dy -= 1f;

        var len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len > 0f)
        {
            dx /= len; dy /= len;
            FacingAngle = (float)Math.Atan2(dy, dx);
            Position = new System.Numerics.Vector2(
                Position.X + dx * BaseMoveSpeed * deltaSeconds,
                Position.Y + dy * BaseMoveSpeed * deltaSeconds);
        }
    }

    /// <summary>火力更新（FireCooldown管理）</summary>
    public void UpdateFireCooldown(float deltaSeconds)
    {
        if (FireCooldown > 0f) FireCooldown = Math.Max(0f, FireCooldown - deltaSeconds);
    }

    /// <summary>スキルクールダウン更新</summary>
    public void UpdateSkillCooldowns(float deltaSeconds)
    {
        if (SkillOneCooldown > 0f) SkillOneCooldown = Math.Max(0f, SkillOneCooldown - deltaSeconds);
        if (SkillTwoCooldown > 0f) SkillTwoCooldown = Math.Max(0f, SkillTwoCooldown - deltaSeconds);
    }
}
