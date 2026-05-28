namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private static void ResetActorAbilityState(Actor actor)
    {
        actor.SkillOneCooldown = 0f;
        actor.SkillTwoCooldown = 0f;
        actor.UltimateCharge = 0f;
        actor.AbilityThinkCooldown = 0.2f;
        actor.DashTimer = 0f;
        actor.OverdriveTimer = 0f;
        actor.HealingTimer = 0f;
        actor.GhostTimer = 0f;
    }

    private void UpdateActorAbilityState(Actor actor, float deltaSeconds)
    {
        if (actor.Type == ActorType.Player)
        {
            return;
        }

        actor.SkillOneCooldown = MathF.Max(0f, actor.SkillOneCooldown - deltaSeconds);
        actor.SkillTwoCooldown = MathF.Max(0f, actor.SkillTwoCooldown - deltaSeconds);
        actor.AbilityThinkCooldown = MathF.Max(0f, actor.AbilityThinkCooldown - deltaSeconds);
        actor.DashTimer = MathF.Max(0f, actor.DashTimer - deltaSeconds);
        actor.OverdriveTimer = MathF.Max(0f, actor.OverdriveTimer - deltaSeconds);
        actor.GhostTimer = MathF.Max(0f, actor.GhostTimer - deltaSeconds);

        if (actor.HealingTimer > 0f && actor.IsAlive)
        {
            actor.Health = MathF.Min(actor.MaxHealth, actor.Health + (7f * deltaSeconds));
            actor.HealingTimer = MathF.Max(0f, actor.HealingTimer - deltaSeconds);
        }

        var chargeRate = actor.IsBoss ? 0.065f : 0.045f;
        actor.UltimateCharge = Math.Clamp(actor.UltimateCharge + (deltaSeconds * chargeRate), 0f, MaxUltPoints);
    }

    private void TryUseAutonomousAgentAbility(Actor actor, Actor? target, float deltaSeconds)
    {
        if (actor.Type == ActorType.Player || !actor.IsAlive || actor.AbilityThinkCooldown > 0f)
        {
            return;
        }

        actor.AbilityThinkCooldown = 0.38f + ((float)_random.NextDouble() * 0.42f);
        if (IsActorSystemCrashed(actor) || IsActorLockedDown(actor))
        {
            return;
        }

        var targetPoint = target?.Position ?? (actor.Type == ActorType.Enemy ? CellCenter(PickPathGoal(actor)) : BombSitePosition(_attackFocusSite));
        var targetDistance = target is null ? float.MaxValue : Distance(actor.Position, target.Position);
        var shouldUlt = actor.UltimateCharge >= MaxUltPoints &&
            (target is not null && targetDistance <= 210f || actor.Health <= actor.MaxHealth * 0.45f);

        if (shouldUlt && UseAutonomousAbility(actor, AgentAbilitySlot.Ultimate, target, targetPoint))
        {
            actor.UltimateCharge = 0f;
            return;
        }

        if (actor.SkillTwoCooldown <= 0f && ShouldUseAutonomousSkillTwo(actor, target, targetDistance) &&
            UseAutonomousAbility(actor, AgentAbilitySlot.SkillTwo, target, targetPoint))
        {
            actor.SkillTwoCooldown = AgentSkillTwoCooldownSeconds + ((float)_random.NextDouble() * 2.4f);
            return;
        }

        if (actor.SkillOneCooldown <= 0f && ShouldUseAutonomousSkillOne(actor, target, targetDistance) &&
            UseAutonomousAbility(actor, AgentAbilitySlot.SkillOne, target, targetPoint))
        {
            actor.SkillOneCooldown = AgentSkillOneCooldownSeconds + ((float)_random.NextDouble() * 1.8f);
        }

        _ = deltaSeconds;
    }

    private bool ShouldUseAutonomousSkillOne(Actor actor, Actor? target, float targetDistance)
    {
        return actor.Agent switch
        {
            AgentKind.Nitro => target is null || targetDistance > 150f,
            AgentKind.Oasis => target is not null && targetDistance <= 220f,
            AgentKind.Divide => target is not null && targetDistance <= 170f,
            AgentKind.Glitch => target is not null && targetDistance <= 260f,
            _ => target is not null && targetDistance <= 230f,
        };
    }

    private bool ShouldUseAutonomousSkillTwo(Actor actor, Actor? target, float targetDistance)
    {
        return actor.Agent switch
        {
            AgentKind.Veil => target is not null && targetDistance <= 180f,
            AgentKind.Vine => target is not null && targetDistance <= 190f,
            AgentKind.Nitro => target is not null && targetDistance <= 112f,
            AgentKind.Oasis => actor.Health <= actor.MaxHealth * 0.72f,
            AgentKind.Divide => target is null || targetDistance <= 220f,
            AgentKind.Glitch => target is not null && targetDistance <= 190f,
            _ => false,
        };
    }

    private bool UseAutonomousAbility(Actor actor, AgentAbilitySlot slot, Actor? target, PointF targetPoint)
    {
        return actor.Agent switch
        {
            AgentKind.Veil => UseAutonomousVeilAbility(actor, slot, targetPoint),
            AgentKind.Vine => UseAutonomousVineAbility(actor, slot, target, targetPoint),
            AgentKind.Nitro => UseAutonomousNitroAbility(actor, slot, target, targetPoint),
            AgentKind.Oasis => UseAutonomousOasisAbility(actor, slot, targetPoint),
            AgentKind.Divide => UseAutonomousDivideAbility(actor, slot, targetPoint),
            AgentKind.Glitch => UseAutonomousGlitchAbility(actor, slot, target, targetPoint),
            _ => false,
        };
    }

    private bool UseAutonomousVeilAbility(Actor actor, AgentAbilitySlot slot, PointF targetPoint)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => AddAutonomousWorldEffect(actor, WorldEffectKind.PoisonCloud, targetPoint, 88f, 4.8f, "毒霧弾"),
            AgentAbilitySlot.SkillTwo => TryPlaceAutonomousTemporaryStructure(actor, BuildToolKind.PortableCover, actor.Position, 8f, "防弾カバー"),
            AgentAbilitySlot.Ultimate => AddAutonomousWorldEffect(actor, WorldEffectKind.DeadlyDome, targetPoint, 108f, 7f, "致死ドーム"),
            _ => false,
        };
    }

    private bool UseAutonomousVineAbility(Actor actor, AgentAbilitySlot slot, Actor? target, PointF targetPoint)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                if (target is null)
                {
                    return false;
                }

                if (target.Type == ActorType.Enemy)
                {
                    RevealEnemyToTeam(target, SharedVisionDurationSeconds + 1.8f);
                }

                EmitRipple(target.Position, 0.78f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                AnnounceAutonomousAbility(actor, "ソナー矢");
                return true;
            case AgentAbilitySlot.SkillTwo:
                return AddAutonomousWorldEffect(actor, WorldEffectKind.SilenceZone, actor.Position, 130f, 6f, "サイレンスゾーン");
            case AgentAbilitySlot.Ultimate:
                AddAutonomousWorldEffect(actor, WorldEffectKind.HunterEye, actor.Position, 185f, 7f, "ハンターズアイ");
                if (actor.Type != ActorType.Enemy)
                {
                    RevealEnemiesInActorVision(actor, SharedVisionDurationSeconds + 2.4f);
                }

                return true;
            default:
                return false;
        }
    }

    private bool UseAutonomousNitroAbility(Actor actor, AgentAbilitySlot slot, Actor? target, PointF targetPoint)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                actor.DashTimer = 0.65f;
                AnnounceAutonomousAbility(actor, "瞬間加速");
                EmitRipple(actor.Position, 0.66f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                return true;
            case AgentAbilitySlot.SkillTwo:
                if (target is null || Distance(actor.Position, target.Position) > 124f)
                {
                    return false;
                }

                ApplyDamage(target, 28f, actor);
                EmitRipple(targetPoint, 1.02f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                AnnounceAutonomousAbility(actor, "インパクトボム");
                return true;
            case AgentAbilitySlot.Ultimate:
                actor.OverdriveTimer = 9f;
                AnnounceAutonomousAbility(actor, "オーバードライブ");
                EmitRipple(actor.Position, 0.88f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                return true;
            default:
                return false;
        }
    }

    private bool UseAutonomousOasisAbility(Actor actor, AgentAbilitySlot slot, PointF targetPoint)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                return AddAutonomousWorldEffect(actor, WorldEffectKind.NanoSmoke, targetPoint, 104f, 6f, "ナノスモーク");
            case AgentAbilitySlot.SkillTwo:
                actor.HealingTimer = 4.5f;
                AnnounceAutonomousAbility(actor, "再生ナノマシン");
                EmitRipple(actor.Position, 0.58f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                return true;
            case AgentAbilitySlot.Ultimate:
                actor.Shield = MathF.Min(actor.MaxShield + 25f, actor.Shield + 25f);
                AnnounceAutonomousAbility(actor, "オーバーシールド");
                EmitRipple(actor.Position, 0.78f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                return true;
            default:
                return false;
        }
    }

    private bool UseAutonomousDivideAbility(Actor actor, AgentAbilitySlot slot, PointF targetPoint)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => TryPlaceAutonomousTemporaryStructure(actor, BuildToolKind.HoneyTrap, targetPoint, 15f, "拘束トラップ"),
            AgentAbilitySlot.SkillTwo => TryPlaceAutonomousTemporaryStructure(actor, BuildToolKind.ReconBeacon, actor.Position, 14f, "警告センサー"),
            AgentAbilitySlot.Ultimate => AddAutonomousWorldEffect(actor, WorldEffectKind.Lockdown, targetPoint, 156f, 9f, "ロックダウン"),
            _ => false,
        };
    }

    private bool UseAutonomousGlitchAbility(Actor actor, AgentAbilitySlot slot, Actor? target, PointF targetPoint)
    {
        switch (slot)
        {
            case AgentAbilitySlot.SkillOne:
                if (target is null)
                {
                    return false;
                }

                if (target.Type == ActorType.Enemy)
                {
                    RevealEnemyToTeam(target, SharedVisionDurationSeconds + 2.4f);
                }

                EmitRipple(target.Position, 0.92f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                AnnounceAutonomousAbility(actor, "索敵ペット");
                return true;
            case AgentAbilitySlot.SkillTwo:
                actor.GhostTimer = 2.8f;
                AnnounceAutonomousAbility(actor, "ゴースト・ムーブ");
                EmitRipple(actor.Position, 0.52f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
                return true;
            case AgentAbilitySlot.Ultimate:
                return AddAutonomousWorldEffect(actor, WorldEffectKind.SystemCrash, actor.Position, 210f, 7f, "システム・クラッシュ");
            default:
                return false;
        }
    }

    private bool AddAutonomousWorldEffect(Actor actor, WorldEffectKind kind, PointF position, float radius, float lifetime, string label)
    {
        var profile = AgentCatalog.Get(actor.Agent);
        var used = AddWorldEffect(kind, position, radius, lifetime, profile.Accent, string.Empty, actor.Type);
        if (used)
        {
            EmitRipple(actor.Position, 0.82f, RippleKind.Skill, profile.Accent);
            AnnounceAutonomousAbility(actor, label);
        }

        return used;
    }

    private bool TryPlaceAutonomousTemporaryStructure(Actor actor, BuildToolKind tool, PointF targetPoint, float lifetime, string label)
    {
        var used = TryPlaceTemporaryStructure(tool, targetPoint, lifetime, string.Empty, actor.Type);
        if (used)
        {
            EmitRipple(actor.Position, 0.72f, RippleKind.Skill, AgentCatalog.Get(actor.Agent).Accent);
            AnnounceAutonomousAbility(actor, label);
        }

        return used;
    }

    private void AnnounceAutonomousAbility(Actor actor, string abilityName)
    {
        if (_activityFeed.Count > 0 && _activityFeed[0].Contains($"{actor.Name} が {abilityName}"))
        {
            return;
        }

        PushActivityFeed($"{actor.Name} が {abilityName} を使用。");
    }
}
