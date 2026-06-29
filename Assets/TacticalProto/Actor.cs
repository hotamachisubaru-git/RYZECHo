using System;
using System.Collections.Generic;

namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// アクターの状態（HP、シールド、位置、ステータス）。
    /// 既存のActorModels.csのActorクラスを参考にした純粋C#モデル。
    /// </summary>
    public sealed class Actor
    {
        public string Name { get; init; } = "";
        public AgentKind Agent { get; set; }
        public ActorType Type { get; init; }

        // 位置
        public Point HomeCell { get; init; }
        public PointF Position { get; set; }
        public float Radius { get; init; }

        // HP / シールド
        public float MaxHealth { get; init; }
        public float Health { get; set; }
        public float MaxShield { get; init; }
        public float Shield { get; set; }
        public float ShieldRegenDelay { get; set; }

        // 感知 / 移動
        public float HearingRange { get; init; }
        public float BaseMoveSpeed { get; init; }
        public float FacingAngle { get; set; }

        // クールダウン
        public float FireCooldown { get; set; }
        public float SkillOneCooldown { get; set; }
        public float SkillTwoCooldown { get; set; }
        public float UltimateCharge { get; set; }

        // ステータスバッファ / デバフ
        public float DashTimer { get; set; }
        public float OverdriveTimer { get; set; }
        public float HealingTimer { get; set; }
        public float GhostTimer { get; set; }
        public bool IsSilenced { get; set; }
        public bool IsBoss { get; set; }

        // パス
        public Queue<PointF> Path { get; } = new();

        // 状態判定
        public bool IsAlive => Health > 0.01f;
        public bool HasShield => Shield > 0.01f;
        public float TotalEffectiveHealth => Health + Shield;

        public Actor() { }

        public Actor(string name, ActorType type, float maxHealth, float maxShield)
        {
            Name = name;
            Type = type;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MaxShield = maxShield;
            Shield = maxShield;
        }

        /// <summary>ダメージを受ける。シールドが先に吸収。</summary>
        public float TakeDamage(float damage)
        {
            if (!IsAlive) return 0f;

            float remaining = damage;
            if (Shield > 0f)
            {
                var absorbed = Math.Min(Shield, remaining);
                Shield -= absorbed;
                remaining -= absorbed;
            }
            Health -= remaining;
            if (Health < 0f) Health = 0f;
            return damage;
        }

        /// <summary>シールドを再生する。</summary>
        public void RegenShield(float amount, float delaySeconds)
        {
            ShieldRegenDelay = delaySeconds;
        }

        /// <summary>ヘルスを回復。</summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health = Math.Min(MaxHealth, Health + amount);
        }

        /// <summary>最終的な死亡判定。</summary>
        public bool IsDead() => Health <= 0f;

        /// <summary>キャラクターの概要を文字列化。</summary>
        public string Summary() =>
            $"{Name}[{Type}] HP={Health:F0}/{MaxHealth:F0} SH={Shield:F0}/{MaxShield:F0}";
    }

    // --- RYZECHo.GameEnums の enum を using RYZECHo; で参照 ---
    // AgentKind, ActorType, GamePhase, WeaponType, TeamRole, BuildToolKind, WorldEffectKind, StructureKind, RippleKind

    // --- 純粋几何型（UnityEngine依存なし） ---

    public readonly record struct Point(int X, int Y);
    public record struct PointF(float X, float Y);
}
