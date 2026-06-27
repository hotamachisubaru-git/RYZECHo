namespace RYZECHo;

internal sealed partial class GameModel
{
    private void StartRound()
    {
        EnsureBossSelectionAvailable();
        EnsureFriendlyEconomyState();
        SyncSelectedBetTotal();

        var primaryWeapon = _weaponStats[_selectedWeapon];
        var sidearmWeapon = _weaponStats[_selectedSidearmWeapon];
        var totalCost = primaryWeapon.Cost + sidearmWeapon.Cost + _selectedBet;
        if (totalCost > _credits)
        {
            SetResultMessage("髫ｰ繝ｻﾂ髫ｰ蝠上・邵ｺ驢搾ｽｹ譎｢・ｽ・ｬ驛｢・ｧ繝ｻ・ｸ驛｢譏ｴ繝ｻ郢晢ｽｨ驍ｵ・ｺ霑ｹ螟ｲ・ｽ・ｶ繝ｻ・ｳ驛｢・ｧ驗呻ｽｫ遶擾ｽｪ驍ｵ・ｺ陝ｶ蜻ｻ・ｽ骰具ｽｸ・ｲ郢ｧ莠･繝ｻ鬮ｮ蟲ｨ繝ｻ繝ｻ・｡鬮ｦ・ｪ・ゑｽｰ鬮ｯ・ｬ郢晢ｽｻ繝ｻ蜥擾ｽｹ・ｧ陞ｳ螟ｲ・ｽ・ｦ霑｢諤懶ｽｳ・ｩ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驍ｵ・ｺ闕ｳ蟯ｩ蜻ｳ驍ｵ・ｺ髴郁ｲｻ・ｼ讓抵ｽｸ・ｲ郢晢ｽｻ);
            return;
        }

        _credits -= totalCost;
        _bossSelectionCounts[_selectedBossName] = GetBossSelectionCount(_selectedBossName) + 1;
        _player.Agent = _selectedAgent;
        AwardRoundStartUltPoints();
        ResetAgentRuntimeState(clearWorldEffects: true);
        _roundBossKillCount = 0;
        _enemyBossInvestment = 0;
        _playerIdleSeconds = 0f;
        _breathingRippleCooldown = 0f;
        _adImpressionTimer = 0f;
        _coreHealth = 180f;
        _bombPlanted = false;
        _armedBombSiteId = null;
        _attackFocusSite = ChooseAttackFocusSite();
        _bombPlantProgress = 0f;
        _bombDefuseProgress = 0f;
        _activePlanter = null;
        _playerPrimaryWeapon = _selectedWeapon;
        _playerSidearmWeapon = _selectedSidearmWeapon;
        _selectedLoadoutFocus = LoadoutFocus.Primary;
        _player.Weapon = _playerPrimaryWeapon;
        _player.Health = _player.MaxHealth;
        _player.Shield = _player.MaxShield;
        _player.ShieldRegenDelay = 0f;
        _player.Position = CellCenter(_player.HomeCell);
        _player.FireCooldown = 0f;
        _player.FootstepCooldown = 0f;
        _player.FootstepPulseIndex = 0;
        _player.PathCooldown = 0f;
        _player.Path.Clear();
        ResetActorAbilityState(_player);

        foreach (var actor in _allies)
        {
            actor.Health = actor.MaxHealth;
            actor.Shield = actor.MaxShield;
            actor.ShieldRegenDelay = 0f;
            actor.Position = CellCenter(actor.HomeCell);
            actor.FireCooldown = 0f;
            actor.FootstepCooldown = 0f;
            actor.FootstepPulseIndex = 0;
            actor.PathCooldown = 0f;
            actor.Path.Clear();
            ResetActorAbilityState(actor);
        }

        foreach (var structure in _structures.Where(structure => IsRouteBlockingStructure(structure.Kind)))
        {
            structure.Health = Math.Clamp(structure.Health, 0f, structure.MaxHealth);
        }

        _enemies.Clear();
        _worldEffects.Clear();
        _ripples.Clear();
        ResetSharedVision();
        CreateEnemySquad();
        _roundTimer = RoundDurationSeconds;
        _pingCooldown = 0f;
        ArmIntegrityGrace();

        RestoreBossFlags();
        _phase = GamePhase.Hunt;
        _eventBus.Emit(new GamePhaseChangedEvent(GamePhase.Hunt));
        _showBriefing = false;
        _resultDestination = GamePhase.Bet;
        SetResultMessage(IsPlayerTeamAttacking()
            ? $"鬮ｫ・ｨ繝ｻ・ｬ{_currentRound}驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎∝ｴ溯濤謌頑ｲり嵯謨鳴郢ｧ莠･諢幃垈・ｦ郢晢ｽｻ郢晢ｽｻ驍ｵ・ｺ繝ｻ・ｨ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驛｢・ｧ繝ｻ・ｵ驛｢・ｧ繝ｻ・､驛｢譏ｴ繝ｻ{_attackFocusSite switch { ObjectiveSiteId.Alpha => "A", _ => "B" }} 驛｢・ｧ陷代・・ｽ・ｸ繝ｻ・ｻ鬮ｴ繝ｻ・ｽ・ｸ驍ｵ・ｺ繝ｻ・ｫ鬯ｨ・ｾ繝ｻ・ｲ髯ｷ闌ｨ・ｽ・･驍ｵ・ｺ陷会ｽｱ・つ遶丞｣ｹ繝ｻ驛｢譎｢・｣・ｰ驛｢・ｧ陞ｳ螟ｲ・ｽ・ｨ繝ｻ・ｭ鬩励ｑ・ｽ・ｮ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驍ｵ・ｺ闕ｳ蟯ｩ蜻ｳ驍ｵ・ｺ髴郁ｲｻ・ｼ讓抵ｽｸ・ｲ郢ｧ莨夲ｽｽ・ｷ闕ｵ諤懊・鬮ｮ蟲ｨ繝ｻ{_selectedBet}c / 驛｢譎・鯵邵ｺ蟷・ｽｬ螢ｽ繝ｻ繝ｻ・ｳ郢晢ｽｻ{SelectedBossInvestment()}c驍ｵ・ｲ郢晢ｽｻ
            : $"鬮ｫ・ｨ繝ｻ・ｬ{_currentRound}驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎∝ｴ溯濤謌頑ｲり嵯謨鳴郢ｧ蛟ｶ・ｺ貊・距陝ｶ蟶吶・驍ｵ・ｺ繝ｻ・ｨ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ A/B 驛｢・ｧ繝ｻ・ｵ驛｢・ｧ繝ｻ・､驛｢譎冗樟繝ｻ螳壽･懆峪・ｻ繝ｻ鬘費ｽｸ・ｲ遶擾ｽｬ繝ｻ・ｨ繝ｻ・ｭ鬩励ｑ・ｽ・ｮ驛｢・ｧ陝ｶ譏懶ｽｻ繝ｻ・ｱ繝ｻ・ｽ・｢驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驍ｵ・ｺ闕ｳ蟯ｩ蜻ｳ驍ｵ・ｺ髴郁ｲｻ・ｼ讓抵ｽｸ・ｲ郢ｧ莨夲ｽｽ・ｷ闕ｵ諤懊・鬮ｮ蟲ｨ繝ｻ{_selectedBet}c / 驛｢譎・鯵邵ｺ蟷・ｽｬ螢ｽ繝ｻ繝ｻ・ｳ郢晢ｽｻ{SelectedBossInvestment()}c驍ｵ・ｲ郢晢ｽｻ);
    }

    private void EndRound(bool won, string? outcomeSummary = null)
    {
        var bossAlive = SelectedBoss()?.IsAlive ?? false;
        var bossPayout = BossEconomyRules.CalculateRoundPayout(
            _bossInvestments,
            won,
            bossAlive,
            _roundBossKillCount,
            BossPayoutMultiplier);
        var completedRound = _currentRound;
        var integrityLocked = IsIntegrityRewardsLocked();

        if (won)
        {
            _playerRoundWins++;
        }
        else
        {
            _enemyRoundWins++;
        }

        var economySummary = integrityLocked
            ? "髫ｰ・ｨ繝ｻ・ｴ髯ｷ・ｷ陜捺ｷ楪繝ｻ・ｧ鬯ｩ蜍滉ｾ幄ｲょ､奇ｽｮﾂ隲帙・・｡蜥ｲ・ｸ・ｺ繝ｻ・ｫ驛｢・ｧ陋ｹ・ｻ繝ｻ鬘費ｽｹ譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎臥櫨繝ｻ・ｰ繝ｻ・ｱ鬯ｩ貊ゑｽｽ・ｬ髯ｷ繝ｻ隱ｿ繝ｻ・ｵ郢晢ｽｻ
            : won
                ? $"髯ｷ閧ｴ蜑碁洛諛・捗繝ｻ・ｱ鬯ｩ貊ゑｽｽ・ｬ +{WinRewardCredits}c"
                : $"髫ｰ・ｨ隲､諛ｷ諱滄ｫｯ・ｬ隲幢ｽｷ隨渉 +{LossRewardCredits}c";

        if (!integrityLocked)
        {
            _credits += won ? WinRewardCredits : LossRewardCredits;

            if (bossPayout.TotalInvestedCredits > 0)
            {
                if (bossPayout.InvestmentReturned)
                {
                    _credits += bossPayout.TotalReturnedCredits;
                    economySummary += $" / 髫ｰ螢ｽ繝ｻ繝ｻ・ｳ郢晢ｽｻ繝ｻ・ｿ驕伜∞・ｽ繝ｻ+{bossPayout.TotalReturnedCredits}c";
                    PushActivityFeed($"髫ｰ螢ｽ繝ｻ繝ｻ・ｳ郢晢ｽｻ繝ｻ・ｿ驕伜∞・ｽ繝ｻ・ｩ貊難ｽｦ鄙ｫ繝ｻ: {FormatBossReturnAllocation(bossPayout)}");
                }
                else
                {
                    economySummary += $" / {bossPayout.Reason}";
                }
            }
        }

        var resultSummary = $"{outcomeSummary ?? (won ? "驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎臥櫨闔・ｫ髯具ｽｻ繝ｻ・ｩ驍ｵ・ｲ郢晢ｽｻ : "驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎牙愛鬯夲ｽｨ髯具ｽｹ陷会ｽｱ・つ郢晢ｽｻ)} {economySummary}驍ｵ・ｲ郢晢ｽｻ;

        if (HasMatchWinner())
        {
            _resultDestination = won ? GamePhase.Victory : GamePhase.Defeat;
            AwardMatchProgression(won);
            SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}驍ｵ・ｲ郢晢ｽｻ_lastProgressionSummary}");
        }
        else
        {
            _currentRound++;

            var enteredOvertime = !_isOvertime && _playerRoundWins == OvertimeTriggerScore && _enemyRoundWins == OvertimeTriggerScore;
            if (enteredOvertime)
            {
                _isOvertime = true;
            }

            if (completedRound == RegulationSideSwitchRound)
            {
                _playerTeamRole = ToggleRole(_playerTeamRole);
                _resultDestination = GamePhase.Construct;
                _sideSwapConstructPending = true;
                _buildPoints = MapEditApRules.RefillForEditPhase(_buildPoints, SideSwapBuildPointRefill, MaxBuildPoints).AfterAp;
                SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}驍ｵ・ｲ郢ｧ莨夲ｽｽ・ｬ繝ｻ・ｬ4驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎√・繝ｻ・ｵ郢ｧ繝ｻ・ｽ・ｺ郢晢ｽｻ遶企豪・ｸ・ｺ繝ｻ・､驍ｵ・ｺ髢ｧ・ｴ陋ｻ・､髯橸ｽｳ闔蛹・ｽｽ・ｺ繝ｻ・､髣比ｼ夲ｽｽ・｣驍ｵ・ｲ遶乗亢繝ｻ驛｢・ｧ繝ｻ・ｨ驛｢譏ｴ繝ｻ邵ｺ繝ｻ・ｹ譏ｴ繝ｻ郢晢ｽｨ驛｢・ｧ陝ｶ譎擾ｽｹ謌頑ｲり嵯譎｢・ｼ・ｰ驍ｵ・ｺ繝ｻ・ｾ驍ｵ・ｺ陷ｷ・ｶ・つ郢晢ｽｻ);
            }
            else
            {
                _resultDestination = GamePhase.Bet;
                if (enteredOvertime)
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}驍ｵ・ｲ郢晢ｽｻ-6 驍ｵ・ｺ繝ｻ・ｮ驍ｵ・ｺ雋・∞・ｽ竏ｫ・ｹ・ｧ繝ｻ・ｪ驛｢譎｢・ｽ・ｼ驛｢譎√・郢晢ｽｻ驛｢・ｧ繝ｻ・ｿ驛｢・ｧ繝ｻ・､驛｢譎｢・｣・ｰ驍ｵ・ｺ繝ｻ・ｫ鬩包ｽｯ遶乗亢繝ｻ驍ｵ・ｺ陷会ｽｱ遶擾ｽｪ驍ｵ・ｺ陷ｷ・ｶ・つ郢晢ｽｻ);
                }
                else
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}驍ｵ・ｲ郢晢ｽｻ);
                }
            }
        }

        ResetAgentRuntimeState(clearWorldEffects: true);
        _phase = GamePhase.RoundResult;
        _eventBus.Emit(new GamePhaseChangedEvent(GamePhase.RoundResult));
        _resultTimer = 2.4f;
    }

    private static string FormatBossReturnAllocation(BossRoundPayout payout)
    {
        var allocations = payout.Returns
            .Where(entry => entry.ReturnedCredits > 0)
            .Select(entry => $"{entry.InvestorName}+{entry.ReturnedCredits}c")
            .ToArray();

        return allocations.Length == 0 ? payout.Reason : string.Join(" / ", allocations);
    }

    private void BeginBetPhase()
    {
        _phase = GamePhase.Bet;
        _eventBus.Emit(new GamePhaseChangedEvent(GamePhase.Bet));
        EnsureBossSelectionAvailable();
        EnsureFriendlyEconomyState();
        SyncSelectedBetTotal();
        _selectedLoadoutFocus = LoadoutFocus.Primary;
        _resultDestination = GamePhase.Bet;
        _bombPlanted = false;
        _armedBombSiteId = null;
        _bombPlantProgress = 0f;
        _bombDefuseProgress = 0f;
        _activePlanter = null;
        _agentSkillPurchased = false;
        ResetAgentRuntimeState(clearWorldEffects: true);
        SetResultMessage($"鬮ｫ・ｨ繝ｻ・ｬ{_currentRound}驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎牙愛繝ｻ・ｺ鬮｢ﾂ繝ｻ蜥擾ｽｸ・ｲ郢晢ｽｻPlayerRoleLabel()}驍ｵ・ｺ繝ｻ・ｨ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驛｢譎・鯵邵ｺ蟶ｷ・ｸ・ｲ遶擾ｽｫ繝ｻ・ｷ闕ｵ諤懊・鬮ｮ蟲ｨ繝ｻ繝ｻ・｡鬮ｦ・ｪ・つ遶擾ｽｵ繝ｻ・ｭ繝ｻ・ｦ髯懆ｶ｣・ｽ・ｨ驛｢・ｧ陷ｻ闌ｨ・ｽ・ｱ繝ｻ・ｺ驛｢・ｧ遶丞｣ｺﾂ・ｻ驍ｵ・ｺ闕ｳ蟯ｩ蜻ｳ驍ｵ・ｺ髴郁ｲｻ・ｼ讓抵ｽｸ・ｲ郢晢ｽｻ);
    }

    private void ResetCampaign()
    {
        _buildPoints = InitialBuildPoints;
        _credits = StartingCredits;
        _currentRound = 1;
        _playerRoundWins = 0;
        _enemyRoundWins = 0;
        _selectedBet = OptimalBossInvestment;
        _selectedWeapon = WeaponType.Giant;
        _selectedSidearmWeapon = WeaponType.Pulse;
        _playerPrimaryWeapon = WeaponType.Giant;
        _playerSidearmWeapon = WeaponType.Pulse;
        _selectedLoadoutFocus = LoadoutFocus.Primary;
        _selectedBuildTool = BuildToolKind.BlastDoor;
        _selectedAgent = AgentKind.Veil;
        _agentSkillPurchased = false;
        _selectedBossName = RosterCatalog.PlayerName;
        _coreHealth = 180f;
        _bombPlanted = false;
        _armedBombSiteId = null;
        _bombPlantProgress = 0f;
        _bombDefuseProgress = 0f;
        _activePlanter = null;
        _isOvertime = false;
        _sideSwapConstructPending = false;
        _playerIdleSeconds = 0f;
        _breathingRippleCooldown = 0f;
        _playerTeamRole = TeamRole.Defense;
        _phase = GamePhase.Construct;
        _eventBus.Emit(new GamePhaseChangedEvent(GamePhase.Construct));
        _resultDestination = GamePhase.Bet;
        _showBriefing = true;
        _enemyBossInvestment = 0;
        _matchTeamEliminations = 0;
        _matchPlayerDeaths = 0;
        _roundBossKillCount = 0;
        _adImpressionTimer = 0f;
        ResetAgentRuntimeState(clearWorldEffects: true);
        _structures.Clear();
        _worldEffects.Clear();
        _ripples.Clear();
        _enemies.Clear();
        _sharedVisionTimers.Clear();
        _activityFeed.Clear();
        _bossSelectionCounts.Clear();
        _bossInvestments.Clear();
        _ultPoints.Clear();

        foreach (var name in BossCandidateNames())
        {
            _bossSelectionCounts[name] = 0;
            _bossInvestments[name] = name == _player.Name ? OptimalBossInvestment : 0;
            _ultPoints[name] = 0;
        }

        SyncSelectedBetTotal();

        SetResultMessage("鬯ｮ・ｯ繝ｻ・｣髯懶ｽｨ繝ｻ・ｰ髫ｶ蝣､霍昴・・ｯ陝ｲ・ｨ郢晢ｽｻ髣包ｽｳ・つ髯溯ｶ｣・ｽ・ｦ驍ｵ・ｺ繝ｻ・ｰ驍ｵ・ｺ闔会ｽ｣・つ郢ｧ莨夲ｽｽ・ｬ繝ｻ・ｬ1-4驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎擾ｽｳ・ｨ郢晢ｽｻ鬯ｮ・ｦ繝ｻ・ｲ鬮ｯ・ｦ陝ｶ蜊債遶擾ｽｫ繝ｻ・ｬ繝ｻ・ｬ5驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・ｦ驛｢譎｢・ｽ・ｳ驛｢譎・・繝ｻ・ｻ繝ｻ・･鬯ｮ・ｯ鬮ｦ・ｪ郢晢ｽｻ髫ｰ・ｾ繝ｻ・ｻ髫ｰ・ｦ郢晢ｽｻ遶城メ蟠慕ｹ晢ｽｻ繝ｻ鬘假ｽｭ蜴・ｽｽ・ｿ驛｢・ｧ闕ｳ螂・ｽｽ鬘費ｽｸ・ｺ繝ｻ・ｾ驍ｵ・ｺ陷ｷ・ｶ・つ郢ｧ繝ｻ繝ｻ驛｢・ｧ繝ｻ・ｹ髫ｰ螢ｽ繝ｻ繝ｻ・ｳ郢晢ｽｻ郢晢ｽｻ 300 髯ｷﾂ郢晢ｽｻ繝ｻ・ｻ陋滂ｽｩ繝ｻ・ｿ闔会ｽ｣遯ｶ・ｲ髫ｴ蟠｢ﾂ髯ｷ莨夲ｽｽ・ｹ鬩阪・繝ｻ邵ｲ蝣､・ｸ・ｺ陷ｷ・ｶ・つ郢晢ｽｻ);

        _player.Health = _player.MaxHealth;
        _player.Shield = _player.MaxShield;
        _player.ShieldRegenDelay = 0f;
        _player.Position = CellCenter(_player.HomeCell);
        _player.Weapon = _playerPrimaryWeapon;
        _player.Agent = _selectedAgent;
        _player.PathCooldown = 0f;
        _player.Path.Clear();
        ResetActorAbilityState(_player);

        if (_allies.Count >= 3)
        {
            _allies[0].Weapon = RosterCatalog.DefaultFriendlyWeaponFor(_allies[0].Name);
            _allies[1].Weapon = RosterCatalog.DefaultFriendlyWeaponFor(_allies[1].Name);
            _allies[2].Weapon = RosterCatalog.DefaultFriendlyWeaponFor(_allies[2].Name);
        }

        foreach (var ally in _allies)
        {
            ally.Health = ally.MaxHealth;
            ally.Shield = ally.MaxShield;
            ally.ShieldRegenDelay = 0f;
            ally.Position = CellCenter(ally.HomeCell);
            ally.IsBoss = false;
            ally.PathCooldown = 0f;
            ally.Path.Clear();
            ResetActorAbilityState(ally);
        }

        _player.IsBoss = true;
        ResetIntegrityRewardsLock();
        ResetIntegritySession();
    }

    private void EnterSideSwapConstructPhase()
    {
        _phase = GamePhase.Construct;
        _eventBus.Emit(new GamePhaseChangedEvent(GamePhase.Construct));
        _resultDestination = GamePhase.Bet;
        _showBriefing = false;
        ArmIntegrityGrace();
        if (string.IsNullOrWhiteSpace(_resultMessage))
        {
            SetResultMessage("髫ｰ・ｾ繝ｻ・ｻ髯橸ｽｳ闔蛹・ｽｽ・ｺ繝ｻ・､髣比ｼ夲ｽｽ・｣驍ｵ・ｲ郢ｧ繝ｻ繝ｻ驛｢・ｧ繝ｻ・ｨ驛｢譏ｴ繝ｻ邵ｺ繝ｻ・ｹ譏ｴ繝ｻ郢晢ｽｨ驍ｵ・ｺ繝ｻ・ｧ髯溷｢薙＝雎ｼ・ｰ髫ｰ魃会ｽｽ・ｦ驍ｵ・ｺ繝ｻ・ｮ鬯ｩ貅ｷ隱ｿ繝ｻ・ｽ繝ｻ・ｮ驛｢・ｧ陞ｳ螟ｲ・ｽ・ｪ繝ｻ・ｿ髫ｰ・ｨ繝ｻ・ｴ驍ｵ・ｺ陷会ｽｱ遯ｶ・ｻ驍ｵ・ｺ闕ｳ蟯ｩ蜻ｳ驍ｵ・ｺ髴郁ｲｻ・ｼ讓抵ｽｸ・ｲ郢晢ｽｻ);
        }
    }

}
