namespace RYZECHo;

internal sealed partial class GameModel
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

    private bool AddWorldEffect(WorldEffectKind kind, PointF position, float radius, float lifetime, Color color, string message)
    {
        return AddWorldEffect(kind, position, radius, lifetime, color, message, ActorType.Player);
    }

    private bool AddWorldEffect(WorldEffectKind kind, PointF position, float radius, float lifetime, Color color, string message, ActorType ownerType)
    {
        _worldEffects.Add(new WorldEffect
        {
            Kind = kind,
            Position = ClampToWorld(position),
            Radius = radius,
            Lifetime = lifetime,
            Color = color,
            OwnerType = ownerType,
        });

        if (!string.IsNullOrWhiteSpace(message))
        {
            SetResultMessage(message);
        }

        return true;
    }

    private bool TryPlaceTemporaryStructure(BuildToolKind tool, PointF target, float lifetime, string message)
    {
        return TryPlaceTemporaryStructure(tool, target, lifetime, message, ActorType.Player);
    }

    private bool TryPlaceTemporaryStructure(BuildToolKind tool, PointF target, float lifetime, string message, ActorType ownerType)
    {
        var cell = WorldToCell(target);
        if (_permanentWalls.Contains(cell) || _structures.Any(structure => structure.Cell == cell) || IsInsideBombSite(CellCenter(cell), 4f))
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetResultMessage("その位置にはスキル設置できません。");
            }

            return false;
        }

        var structure = CreateStructure(tool, cell);
        structure.RemainingLifetime = lifetime;
        structure.Health = structure.MaxHealth;
        structure.OwnerType = ownerType;
        _structures.Add(structure);
        if (!string.IsNullOrWhiteSpace(message))
        {
            SetResultMessage(message);
        }

        return true;
    }

    private PointF ClampToWorld(PointF point)
    {
        return new PointF(
            Math.Clamp(point.X, WorldBounds.Left + 4f, WorldBounds.Right - 4f),
            Math.Clamp(point.Y, WorldBounds.Top + 4f, WorldBounds.Bottom - 4f));
    }

    private bool IsPlayerSilenced()
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SilenceZone && SameTeamSide(_player.Type, effect.OwnerType) && Distance(_player.Position, effect.Position) <= effect.Radius);
    }

    private bool IsActorLockedDown(Actor actor)
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.Lockdown && !SameTeamSide(actor.Type, effect.OwnerType) && Distance(actor.Position, effect.Position) <= effect.Radius);
    }

    private bool IsActorSystemCrashed(Actor actor)
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SystemCrash && !SameTeamSide(actor.Type, effect.OwnerType) && Distance(actor.Position, effect.Position) <= effect.Radius);
    }

    private bool IsLineBlockedByWorldEffect(PointF start, PointF end)
    {
        return _worldEffects.Any(effect =>
            effect.Kind is WorldEffectKind.NanoSmoke or WorldEffectKind.PoisonCloud &&
            SegmentIntersectsCircle(start, end, effect.Position, effect.Radius));
    }

    private static bool SegmentIntersectsCircle(PointF start, PointF end, PointF center, float radius)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= 0.001f)
        {
            return Distance(start, center) <= radius;
        }

        var t = (((center.X - start.X) * dx) + ((center.Y - start.Y) * dy)) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);
        var closest = new PointF(start.X + (dx * t), start.Y + (dy * t));
        return Distance(closest, center) <= radius;
    }

    private float AgentSkillCooldown(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => _agentSkillOneCooldown,
            AgentAbilitySlot.SkillTwo => _agentSkillTwoCooldown,
            _ => 0f,
        };
    }

    private float AgentAbilityProgress(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => 1f - (_agentSkillOneCooldown / AgentSkillOneCooldownSeconds),
            AgentAbilitySlot.SkillTwo => 1f - (_agentSkillTwoCooldown / AgentSkillTwoCooldownSeconds),
            _ => GetUltPoints(_player.Name) / (float)MaxUltPoints,
        };
    }

    private bool AgentAbilityReady(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => _agentSkillOneCooldown <= 0f,
            AgentAbilitySlot.SkillTwo => _agentSkillTwoCooldown <= 0f,
            _ => GetUltPoints(_player.Name) >= MaxUltPoints,
        };
    }

    private string AgentAbilityName(AgentAbilitySlot slot)
    {
        var profile = PlayerAgentProfile();
        return slot switch
        {
            AgentAbilitySlot.SkillOne => profile.SkillOne,
            AgentAbilitySlot.SkillTwo => profile.SkillTwo,
            _ => profile.Ultimate,
        };
    }

    private string AgentRuntimeSummary()
    {
        var profile = PlayerAgentProfile();
        return $"{profile.Name} / 1:{profile.SkillOne} 2:{profile.SkillTwo} 3:{profile.Ultimate} ULT {GetUltPoints(_player.Name)}/{MaxUltPoints}";
    }

    private bool IsSystemCrashActive()
    {
        return _systemCrashTimer > 0f || _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SystemCrash && effect.OwnerType != ActorType.Enemy);
    }

    private float PlayerDamageMultiplier()
    {
        return _playerOverdriveTimer > 0f ? 1.2f : 1f;
    }

    private IEnumerable<Actor> OpponentsOf(ActorType ownerType)
    {
        return IsFriendlyActorType(ownerType)
            ? _enemies.Where(actor => actor.IsAlive)
            : LivePlayerTeam();
    }

    private Actor? EffectOwnerActor(ActorType ownerType)
    {
        if (ownerType == ActorType.Enemy)
        {
            return _enemies.FirstOrDefault(actor => actor.IsAlive);
        }

        return _player.IsAlive ? _player : _allies.FirstOrDefault(actor => actor.IsAlive);
    }
}
