namespace RYZECHo;

public sealed partial class GameModel
{
    private bool UseVeilAbility(AgentAbilitySlot slot, PointF target)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => AddWorldEffect(WorldEffectKind.PoisonCloud, target, 96f, 5.5f, Color.FromArgb(170, 136, 226, 120), "毒霧弾を展開。範囲内の敵に継続ダメージ。"),
            AgentAbilitySlot.SkillTwo => TryPlaceTemporaryStructure(BuildToolKind.BlastDoor, target, 10f, "防弾壁を展開。10 秒間、射線と移動を遮断。"),
            AgentAbilitySlot.Ultimate => AddWorldEffect(WorldEffectKind.DeadlyDome, target, 116f, 8f, Color.FromArgb(188, 164, 255, 116), "致死ドームを展開。範囲内の敵に高密度 DoT。"),
            _ => false,
        };
    }

    private bool UseVineAbility(AgentAbilitySlot slot, PointF target)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                foreach (var enemy in _enemies.Where(enemy => enemy.IsAlive && Distance(enemy.Position, target) <= 170f))
                {
                    RevealEnemyToTeam(enemy, SharedVisionDurationSeconds + 2.8f);
                    EmitRipple(enemy.Position, 0.72f, RippleKind.Skill, AgentCatalog.Get(AgentKind.Vine).Accent);
                }

                SetResultMessage("ソナー矢を発射。着弾点周辺の敵を共有視界へ送信。");
                return true;
            case AgentAbilitySlot.SkillTwo:
                return AddWorldEffect(WorldEffectKind.SilenceZone, _player.Position, 160f, 8f, Color.FromArgb(110, 124, 228, 255), "サイレンスゾーンを展開。呼吸音と味方側の足音波紋を抑制。");
            case AgentAbilitySlot.Ultimate:
                _hunterEyeTimer = 8f;
                AddWorldEffect(WorldEffectKind.HunterEye, _player.Position, 210f, 8f, Color.FromArgb(92, 124, 228, 255), "ハンターズアイ起動。視野内の敵を長めに共有表示。");
                return true;
            default:
                return false;
        }
    }

    private bool UseNitroAbility(AgentAbilitySlot slot, PointF target)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                _playerDashTimer = 0.7f;
                SetResultMessage("瞬間加速。短時間だけ移動速度が大きく上昇。");
                return true;
            case AgentAbilitySlot.SkillTwo:
                foreach (var enemy in _enemies.Where(enemy => enemy.IsAlive && Distance(enemy.Position, target) <= 92f))
                {
                    ApplyDamage(enemy, 38f, _player);
                    RevealEnemyToTeam(enemy, SharedVisionDurationSeconds);
                }

                EmitRipple(target, 1.05f, RippleKind.Skill, AgentCatalog.Get(AgentKind.Nitro).Accent);
                SetResultMessage("インパクトボムを起爆。範囲内の敵へダメージ。");
                return true;
            case AgentAbilitySlot.Ultimate:
                _playerOverdriveTimer = 12f;
                SetResultMessage("オーバードライブ起動。移動、射撃、ダメージを強化。");
                return true;
            default:
                return false;
        }
    }

    private bool UseOasisAbility(AgentAbilitySlot slot, PointF target)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => AddWorldEffect(WorldEffectKind.NanoSmoke, target, 118f, 8f, Color.FromArgb(145, 116, 232, 172), "ナノスモークを展開。範囲内の射線を遮断。"),
            AgentAbilitySlot.SkillTwo => StartPlayerHeal(),
            AgentAbilitySlot.Ultimate => GrantPlayerOvershield(),
            _ => false,
        };
    }

    private bool StartPlayerHeal()
    {
        _playerHealingTimer = 5f;
        SetResultMessage("再生ナノマシンを起動。5 秒かけて HP を回復。");
        return true;
    }

    private bool GrantPlayerOvershield()
    {
        _player.Shield = MathF.Min(_player.MaxShield + 25f, _player.Shield + 25f);
        SetResultMessage("オーバーシールド発動。シールドを即時 +25。");
        return true;
    }

    private bool UseDivideAbility(AgentAbilitySlot slot, PointF target)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => TryPlaceTemporaryStructure(BuildToolKind.HoneyTrap, target, 18f, "拘束トラップを設置。踏んだ敵を鈍足化し、音を増幅。"),
            AgentAbilitySlot.SkillTwo => TryPlaceTemporaryStructure(BuildToolKind.ReconBeacon, target, 16f, "警告センサーを設置。接近した敵を共有視界へ送信。"),
            AgentAbilitySlot.Ultimate => AddWorldEffect(WorldEffectKind.Lockdown, target, 180f, 12f, Color.FromArgb(142, 230, 194, 88), "ロックダウンを展開。範囲内の敵行動を禁止。"),
            _ => false,
        };
    }

    private bool UseGlitchAbility(AgentAbilitySlot slot, PointF target)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                var nearest = _enemies
                    .Where(enemy => enemy.IsAlive)
                    .OrderBy(enemy => Distance(enemy.Position, target))
                    .FirstOrDefault();
                if (nearest is null)
                {
                    SetResultMessage("索敵ペットの追跡対象がありません。");
                    return true;
                }

                RevealEnemyToTeam(nearest, SharedVisionDurationSeconds + 3.2f);
                EmitRipple(nearest.Position, 0.94f, RippleKind.Skill, AgentCatalog.Get(AgentKind.Glitch).Accent);
                SetResultMessage($"索敵ペットが {nearest.Name} の反応を捕捉。");
                return true;
            case AgentAbilitySlot.SkillTwo:
                _playerGhostTimer = 3f;
                SetResultMessage("ゴースト・ムーブ起動。3 秒間、敵のターゲット選択から外れやすくなります。");
                return true;
            case AgentAbilitySlot.Ultimate:
                _systemCrashTimer = 10f;
                AddWorldEffect(WorldEffectKind.SystemCrash, _player.Position, 240f, 10f, Color.FromArgb(112, 196, 132, 255), "システム・クラッシュ発動。敵射撃と波紋発生を一時停止。");
                return true;
            default:
                return false;
        }
    }
}
