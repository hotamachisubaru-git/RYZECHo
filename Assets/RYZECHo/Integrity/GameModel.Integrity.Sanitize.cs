namespace RYZECHo;

internal sealed partial class GameModel
{
    private void SanitizeGlobalRuntimeState()
    {
        if (_credits < 0 || _credits > IntegrityHardCreditsCap)
        {
            var clampedCredits = Math.Clamp(_credits, 0, IntegrityHardCreditsCap);
            RegisterIntegrityStrike("所持金整合性", $"クレジット {_credits}c を {clampedCredits}c に補正しました。", severe: true);
            _credits = clampedCredits;
        }

        _buildPoints = Math.Clamp(_buildPoints, 0, MaxBuildPoints);
        _currentRound = Math.Clamp(_currentRound, 1, 99);
        _playerRoundWins = Math.Clamp(_playerRoundWins, 0, 99);
        _enemyRoundWins = Math.Clamp(_enemyRoundWins, 0, 99);
        _selectedWeapon = SanitizeWeaponType(_selectedWeapon, WeaponType.Giant);
        _selectedSidearmWeapon = SanitizeWeaponType(_selectedSidearmWeapon, WeaponType.Pulse);
        _playerPrimaryWeapon = SanitizeWeaponType(_playerPrimaryWeapon, WeaponType.Giant);
        _playerSidearmWeapon = SanitizeWeaponType(_playerSidearmWeapon, WeaponType.Pulse);
        _selectedAgent = SanitizeAgentKind(_selectedAgent);
        _selectedBet = Math.Clamp(_selectedBet, 0, IntegrityHardCreditsCap);
        _enemyBossInvestment = Math.Clamp(_enemyBossInvestment, 0, 1_200);
        _matchTeamEliminations = Math.Clamp(_matchTeamEliminations, 0, 999);
        _matchPlayerDeaths = Math.Clamp(_matchPlayerDeaths, 0, 999);
        _roundBossKillCount = Math.Clamp(_roundBossKillCount, 0, TeamSize);
        _roundTimer = Math.Clamp(_roundTimer, 0f, _bombPlanted ? BombFuseSeconds : RoundDurationSeconds);
        _resultTimer = Math.Clamp(_resultTimer, 0f, 4f);
        _pingCooldown = Math.Clamp(_pingCooldown, -0.25f, 3f);
        _coreHealth = Math.Clamp(_coreHealth, 0f, 180f);
        _bombPlantProgress = Math.Clamp(_bombPlantProgress, 0f, BombPlantSeconds);
        _bombDefuseProgress = Math.Clamp(_bombDefuseProgress, 0f, BombDefuseSeconds);
        _playerIdleSeconds = Math.Clamp(_playerIdleSeconds, 0f, 60f);
        _breathingRippleCooldown = Math.Clamp(_breathingRippleCooldown, 0f, BreathingRippleIntervalSeconds);
        _agentSkillOneCooldown = Math.Clamp(_agentSkillOneCooldown, 0f, AgentSkillOneCooldownSeconds);
        _agentSkillTwoCooldown = Math.Clamp(_agentSkillTwoCooldown, 0f, AgentSkillTwoCooldownSeconds);
        _playerDashTimer = Math.Clamp(_playerDashTimer, 0f, 1f);
        _playerOverdriveTimer = Math.Clamp(_playerOverdriveTimer, 0f, 12f);
        _playerHealingTimer = Math.Clamp(_playerHealingTimer, 0f, 5f);
        _playerGhostTimer = Math.Clamp(_playerGhostTimer, 0f, 3f);
        _hunterEyeTimer = Math.Clamp(_hunterEyeTimer, 0f, 8f);
        _systemCrashTimer = Math.Clamp(_systemCrashTimer, 0f, 10f);
        _uiPulseTime = MathF.IEEERemainder(_uiPulseTime, 3600f);
        _adImpressionTimer = Math.Clamp(_adImpressionTimer, 0f, 12f);

        if (_activityFeed.Count > IntegrityMaxActivityFeedEntries)
        {
            _activityFeed.RemoveRange(IntegrityMaxActivityFeedEntries, _activityFeed.Count - IntegrityMaxActivityFeedEntries);
        }

        for (var index = _activityFeed.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(_activityFeed[index]))
            {
                _activityFeed.RemoveAt(index);
            }
        }

        if (_ripples.Count > IntegrityMaxRipples)
        {
            _ripples.RemoveRange(IntegrityMaxRipples, _ripples.Count - IntegrityMaxRipples);
            RegisterIntegrityAnomaly("音イベント整合性", "過剰なリップルを削減しました。");
        }

        if (_worldEffects.Count > 24)
        {
            _worldEffects.RemoveRange(24, _worldEffects.Count - 24);
            RegisterIntegrityAnomaly("スキル効果整合性", "過剰なエージェント効果を削減しました。");
        }

        for (var index = _worldEffects.Count - 1; index >= 0; index--)
        {
            var effect = _worldEffects[index];
            if (!Enum.IsDefined(typeof(WorldEffectKind), effect.Kind) || !Enum.IsDefined(typeof(ActorType), effect.OwnerType) || effect.Radius <= 0f || effect.Radius > 320f || effect.Lifetime <= 0f || effect.Lifetime > 18f)
            {
                _worldEffects.RemoveAt(index);
            }
        }

        for (var index = _ripples.Count - 1; index >= 0; index--)
        {
            var ripple = _ripples[index];
            if (ripple.Lifetime <= 0f || ripple.Lifetime > 2f || ripple.Strength <= 0f || ripple.Strength > 1.5f)
            {
                _ripples.RemoveAt(index);
            }
        }

        if (_enemies.Count > TeamSize)
        {
            var excess = _enemies.Count - TeamSize;
            _enemies.RemoveRange(TeamSize, excess);
            RegisterIntegrityStrike("敵編成整合性", $"敵ユニットが上限を超過していたため {excess} 体を除外しました。", severe: true);
        }

        EnsureBossSelectionCounterShape();
        _selectedBossName = BossCandidateNames().Contains(_selectedBossName) ? _selectedBossName : RosterCatalog.PlayerName;
        EnsureBossSelectionAvailable();
        RestoreBossFlags();

        if (_activePlanter is not null && (!_activePlanter.IsAlive || !IsInsideBombSite(_activePlanter.Position, 18f)))
        {
            _activePlanter = null;
        }
    }

    private void EnsureBossSelectionCounterShape()
    {
        var expectedNames = BossCandidateNames();
        var invalidKeys = _bossSelectionCounts.Keys
            .Where(name => !expectedNames.Contains(name))
            .ToList();

        foreach (var invalidKey in invalidKeys)
        {
            _bossSelectionCounts.Remove(invalidKey);
        }

        foreach (var actorName in expectedNames)
        {
            var current = GetBossSelectionCount(actorName);
            _bossSelectionCounts[actorName] = Math.Clamp(current, 0, MaxBossSelectionsPerActor);
        }
    }

    private void SanitizeStructures()
    {
        if (_structures.Count == 0)
        {
            return;
        }

        var original = _structures.ToList();
        var acceptedCells = new HashSet<Point>();
        _structures.Clear();

        foreach (var structure in original)
        {
            var isTemporaryStructure = structure.RemainingLifetime > 0f;
            if ((!isTemporaryStructure && !_buildSlots.Contains(structure.Cell)) || acceptedCells.Contains(structure.Cell))
            {
                RegisterIntegrityStrike("構築物整合性", $"無効な構築物 {structure.Label} を {structure.Cell.X},{structure.Cell.Y} から除去しました。", severe: true);
                continue;
            }

            if (isTemporaryStructure && _permanentWalls.Contains(structure.Cell))
            {
                RegisterIntegrityAnomaly("構築物整合性", $"一時構築物 {structure.Label} を固定壁セルから除去しました。");
                continue;
            }

            if (!TryCreateCanonicalStructure(structure, out var canonical))
            {
                RegisterIntegrityStrike("構築物整合性", "未定義の構築物を除去しました。", severe: true);
                continue;
            }

            canonical.Health = Math.Clamp(structure.Health, 0f, canonical.MaxHealth);
            canonical.OwnerType = Enum.IsDefined(typeof(ActorType), structure.OwnerType) ? structure.OwnerType : ActorType.Player;
            canonical.PulseCooldown = Math.Clamp(structure.PulseCooldown, 0f, StructurePulseCooldownCap(canonical.Kind));
            canonical.RemainingLifetime = Math.Clamp(structure.RemainingLifetime, 0f, 20f);

            var placementError = isTemporaryStructure ? null : ValidateStructurePlacement(canonical);
            if (placementError is not null)
            {
                RegisterIntegrityStrike("構築物整合性", $"{canonical.Label} を検証で棄却: {placementError}", severe: true);
                continue;
            }

            acceptedCells.Add(canonical.Cell);
            _structures.Add(canonical);
        }
    }

    private bool TryCreateCanonicalStructure(Structure structure, out Structure canonical)
    {
        canonical = structure;
        if (!Enum.IsDefined(typeof(StructureKind), structure.Kind))
        {
            return false;
        }

        canonical = structure.Kind switch
        {
            StructureKind.BlastDoor => CreateStructure(BuildToolKind.BlastDoor, structure.Cell),
            StructureKind.HoneyTrap => CreateStructure(BuildToolKind.HoneyTrap, structure.Cell),
            StructureKind.StaticNest => CreateStructure(BuildToolKind.StaticNest, structure.Cell),
            StructureKind.ReconBeacon => CreateStructure(BuildToolKind.ReconBeacon, structure.Cell),
            StructureKind.ShieldRelay => CreateStructure(BuildToolKind.ShieldRelay, structure.Cell),
            StructureKind.PortableCover => CreateStructure(BuildToolKind.PortableCover, structure.Cell),
            StructureKind.VisorWall => CreateStructure(BuildToolKind.VisorWall, structure.Cell),
            StructureKind.HoloDecoy => CreateStructure(BuildToolKind.HoloDecoy, structure.Cell),
            _ => structure,
        };

        return true;
    }

    private static float StructurePulseCooldownCap(StructureKind kind)
    {
        return kind switch
        {
            StructureKind.StaticNest => 1.2f,
            StructureKind.ReconBeacon => 1.4f,
            StructureKind.ShieldRelay => 1.8f,
            StructureKind.VisorWall => 2f,
            StructureKind.HoloDecoy => 1.4f,
            _ => 0.5f,
        };
    }

    private void SanitizeActors(float deltaSeconds)
    {
        SanitizeActor(_player, deltaSeconds);

        foreach (var ally in _allies)
        {
            SanitizeActor(ally, deltaSeconds);
        }

        foreach (var enemy in _enemies)
        {
            SanitizeActor(enemy, deltaSeconds);
        }

        var enemyBosses = _enemies.Where(enemy => enemy.IsBoss).ToList();
        if (enemyBosses.Count > 1)
        {
            foreach (var extraBoss in enemyBosses.Skip(1))
            {
                extraBoss.IsBoss = false;
            }

            RegisterIntegrityStrike("ボス整合性", "敵ボスが複数いたため 1 体に補正しました。", severe: true);
        }
    }

    private void SanitizeActor(Actor actor, float deltaSeconds)
    {
        actor.Weapon = SanitizeWeaponType(actor.Weapon, DefaultWeaponFor(actor));
        actor.Agent = SanitizeAgentKind(actor.Agent);
        actor.Health = Math.Clamp(actor.Health, 0f, actor.MaxHealth);
        var shieldCap = actor.Type == ActorType.Player && actor.Agent == AgentKind.Oasis
            ? actor.MaxShield + 25f
            : actor.MaxShield;
        actor.Shield = Math.Clamp(actor.Shield, 0f, shieldCap);
        actor.ShieldRegenDelay = Math.Clamp(actor.ShieldRegenDelay, 0f, 6f);
        actor.PathCooldown = Math.Clamp(actor.PathCooldown, -0.25f, 3f);
        actor.FootstepCooldown = Math.Clamp(actor.FootstepCooldown, -0.25f, 2f);
        actor.FootstepPulseIndex = Math.Clamp(actor.FootstepPulseIndex, 0, 2);
        actor.FacingAngle = NormalizeAngle(actor.FacingAngle);
        actor.SkillOneCooldown = Math.Clamp(actor.SkillOneCooldown, 0f, AgentSkillOneCooldownSeconds + 3f);
        actor.SkillTwoCooldown = Math.Clamp(actor.SkillTwoCooldown, 0f, AgentSkillTwoCooldownSeconds + 3f);
        actor.UltimateCharge = Math.Clamp(actor.UltimateCharge, 0f, MaxUltPoints);
        actor.AbilityThinkCooldown = Math.Clamp(actor.AbilityThinkCooldown, 0f, 2f);
        actor.DashTimer = Math.Clamp(actor.DashTimer, 0f, 1f);
        actor.OverdriveTimer = Math.Clamp(actor.OverdriveTimer, 0f, 12f);
        actor.HealingTimer = Math.Clamp(actor.HealingTimer, 0f, 5f);
        actor.GhostTimer = Math.Clamp(actor.GhostTimer, 0f, 3f);

        var weapon = _weaponStats[actor.Weapon];
        var fireCooldownCap = Math.Max(GetActorFireCooldown(actor, weapon.FireCooldown * 1.25f), 1f);
        if (actor.FireCooldown < 0f || actor.FireCooldown > fireCooldownCap)
        {
            actor.FireCooldown = Math.Clamp(actor.FireCooldown, 0f, fireCooldownCap);
        }

        var sanitizedPosition = ResolveCollision(actor.Position, actor.Radius);
        if (Distance(actor.Position, sanitizedPosition) > 0.5f)
        {
            actor.Position = sanitizedPosition;
            RegisterIntegrityAnomaly("位置整合性", $"{actor.Name} の位置をマップ内へ補正しました。");
        }

        if (actor.Path.Count > 32)
        {
            var trimmedPath = actor.Path.Take(32).ToArray();
            actor.Path.Clear();
            foreach (var node in trimmedPath)
            {
                actor.Path.Enqueue(ResolveCollision(node, actor.Radius));
            }

            RegisterIntegrityAnomaly("経路整合性", $"{actor.Name} の経路キューを短縮しました。");
        }

        _ = deltaSeconds;
    }

}
