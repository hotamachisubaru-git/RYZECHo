namespace RYZECHo;

public sealed partial class GameModel
{
    private const float AgentSkillOneCooldownSeconds = 8f;
    private const float AgentSkillTwoCooldownSeconds = 12f;

    private AgentProfile SelectedAgentProfile()
    {
        return AgentCatalog.Get(_selectedAgent);
    }

    private AgentProfile PlayerAgentProfile()
    {
        return AgentCatalog.Get(_player.Agent);
    }

    private void CycleSelectedAgent()
    {
        var order = AgentCatalog.SelectionOrder;
        var index = Array.IndexOf(order, _selectedAgent);
        if (index < 0)
        {
            index = 0;
        }

        _selectedAgent = order[(index + 1) % order.Length];
        _player.Agent = _selectedAgent;
        _agentSkillPurchased = false;
        var profile = SelectedAgentProfile();
        SetResultMessage($"使用エージェントを {profile.Name} ({profile.Role}) に変更。スキル購入状態はリセットされました。");
    }

    private void ResetAgentRuntimeState(bool clearWorldEffects)
    {
        _agentSkillOneCooldown = 0f;
        _agentSkillTwoCooldown = 0f;
        _playerDashTimer = 0f;
        _playerOverdriveTimer = 0f;
        _playerHealingTimer = 0f;
        _playerGhostTimer = 0f;
        _hunterEyeTimer = 0f;
        _systemCrashTimer = 0f;

        if (clearWorldEffects)
        {
            _worldEffects.Clear();
        }
    }

    private void AwardRoundStartUltPoints()
    {
        EnsureFriendlyEconomyState();
        foreach (var actorName in BossCandidateNames())
        {
            var award = BossEconomyRules.CalculateUltAward(actorName, GetUltPoints(actorName), 1, MaxUltPoints, "ラウンド開始");
            _ultPoints[actorName] = award.After;
        }

        PushActivityFeed($"ラウンド開始 ULT +1。あなた {GetUltPoints(_player.Name)}/{MaxUltPoints}");
    }

    private void UpdateAgentRuntime(float deltaSeconds, InputSnapshot input)
    {
        _agentSkillOneCooldown = MathF.Max(0f, _agentSkillOneCooldown - deltaSeconds);
        _agentSkillTwoCooldown = MathF.Max(0f, _agentSkillTwoCooldown - deltaSeconds);
        _playerDashTimer = MathF.Max(0f, _playerDashTimer - deltaSeconds);
        _playerOverdriveTimer = MathF.Max(0f, _playerOverdriveTimer - deltaSeconds);
        _playerGhostTimer = MathF.Max(0f, _playerGhostTimer - deltaSeconds);
        _hunterEyeTimer = MathF.Max(0f, _hunterEyeTimer - deltaSeconds);
        _systemCrashTimer = MathF.Max(0f, _systemCrashTimer - deltaSeconds);

        if (_playerHealingTimer > 0f && _player.IsAlive)
        {
            _player.Health = MathF.Min(_player.MaxHealth, _player.Health + (8f * deltaSeconds));
            _playerHealingTimer = MathF.Max(0f, _playerHealingTimer - deltaSeconds);
        }

        UpdateWorldEffects(deltaSeconds);

        if (!_player.IsAlive)
        {
            return;
        }

        if (_hunterEyeTimer > 0f)
        {
            RevealEnemiesInActorVision(_player, SharedVisionDurationSeconds + 0.6f);
        }

        if (input.Press1)
        {
            TryUseAgentAbility(AgentAbilitySlot.SkillOne, input.MousePosition);
        }
        else if (input.Press2)
        {
            TryUseAgentAbility(AgentAbilitySlot.SkillTwo, input.MousePosition);
        }
        else if (input.Press3)
        {
            TryUseAgentAbility(AgentAbilitySlot.Ultimate, input.MousePosition);
        }
    }

    private void UpdateWorldEffects(float deltaSeconds)
    {
        for (var index = _worldEffects.Count - 1; index >= 0; index--)
        {
            var effect = _worldEffects[index];
            effect.Age += deltaSeconds;

            if (effect.Kind is WorldEffectKind.PoisonCloud or WorldEffectKind.DeadlyDome)
            {
                var damagePerSecond = effect.Kind == WorldEffectKind.DeadlyDome ? 12f : 7f;
                foreach (var actor in OpponentsOf(effect.OwnerType).Where(actor => actor.IsAlive && Distance(actor.Position, effect.Position) <= effect.Radius))
                {
                    ApplyDamage(actor, damagePerSecond * deltaSeconds, EffectOwnerActor(effect.OwnerType));
                }
            }

            if (effect.Age >= effect.Lifetime)
            {
                _worldEffects.RemoveAt(index);
            }
        }
    }

    private void TryUseAgentAbility(AgentAbilitySlot slot, Point mousePosition)
    {
        if (!TryGetWorldPointFromScreen(mousePosition, out var target))
        {
            target = _player.Position;
        }

        if (IsActorSystemCrashed(_player) || IsActorLockedDown(_player))
        {
            SetResultMessage("システム妨害中のためエージェントスキルを使用できません。");
            return;
        }

        var profile = PlayerAgentProfile();
        if (slot is AgentAbilitySlot.SkillOne or AgentAbilitySlot.SkillTwo && !_agentSkillPurchased)
        {
            SetResultMessage($"{SelectedAgentProfile().Name} のシグネチャースキルを購入してください。Betフェーズで 5 を押してください。");
            return;
        }

        if (slot == AgentAbilitySlot.SkillOne && _agentSkillOneCooldown > 0f)
        {
            SetResultMessage($"{profile.SkillOne} は再使用まで {_agentSkillOneCooldown:0.0} 秒。");
            return;
        }

        if (slot == AgentAbilitySlot.SkillTwo && _agentSkillTwoCooldown > 0f)
        {
            SetResultMessage($"{profile.SkillTwo} は再使用まで {_agentSkillTwoCooldown:0.0} 秒。");
            return;
        }

        if (slot == AgentAbilitySlot.Ultimate && !TryConsumePlayerUltimate())
        {
            SetResultMessage($"{profile.Ultimate} には ULT {MaxUltPoints} が必要です。現在 {GetUltPoints(_player.Name)}/{MaxUltPoints}。");
            return;
        }

        var used = _player.Agent switch
        {
            AgentKind.Veil => UseVeilAbility(slot, target),
            AgentKind.Vine => UseVineAbility(slot, target),
            AgentKind.Nitro => UseNitroAbility(slot, target),
            AgentKind.Oasis => UseOasisAbility(slot, target),
            AgentKind.Divide => UseDivideAbility(slot, target),
            AgentKind.Glitch => UseGlitchAbility(slot, target),
            _ => false,
        };

        if (!used)
        {
            return;
        }

        if (slot == AgentAbilitySlot.SkillOne)
        {
            _agentSkillOneCooldown = AgentSkillOneCooldownSeconds;
        }
        else if (slot == AgentAbilitySlot.SkillTwo)
        {
            _agentSkillTwoCooldown = AgentSkillTwoCooldownSeconds;
        }

        EmitRipple(_player.Position, 0.86f, RippleKind.Skill, profile.Accent);
        ArmIntegrityGrace();
    }

    private bool TryConsumePlayerUltimate()
    {
        EnsureFriendlyEconomyState();
        var current = GetUltPoints(_player.Name);
        if (current < MaxUltPoints)
        {
            return false;
        }

        _ultPoints[_player.Name] = 0;
        return true;
    }

    private void TryPurchaseAgentSkill()
    {
        if (_agentSkillPurchased)
        {
            SetResultMessage($"{SelectedAgentProfile().Name} のシグネチャースキルは既に購入済みです。");
            return;
        }

        if (_credits < AgentSkillPurchaseCost)
        {
            SetResultMessage($"スキル購入には {AgentSkillPurchaseCost}c が必要です。現在 {_credits}c。");
            return;
        }

        _credits -= AgentSkillPurchaseCost;
        _agentSkillPurchased = true;
        SetResultMessage($"{SelectedAgentProfile().Name} のシグネチャースキルを購入しました。戦闘フェーズで使用できます。");
        PushActivityFeed($"エージェントスキル購入: {SelectedAgentProfile().Name} -{AgentSkillPurchaseCost}c");
    }
}
