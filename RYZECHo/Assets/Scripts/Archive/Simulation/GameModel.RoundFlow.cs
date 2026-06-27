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
            SetResultMessage("所持クレジットが足りません。投資額か装備を見直してください。");
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
        _showBriefing = false;
        _resultDestination = GamePhase.Bet;
        SetResultMessage(IsPlayerTeamAttacking()
            ? $"第{_currentRound}ラウンド開始。攻撃側としてサイト {_attackFocusSite switch { ObjectiveSiteId.Alpha => "A", _ => "B" }} を主軸に進入し、ボムを設置してください。総投資 {_selectedBet}c / ボス投資 {SelectedBossInvestment()}c。"
            : $"第{_currentRound}ラウンド開始。防衛側として A/B サイトを守り、設置を阻止してください。総投資 {_selectedBet}c / ボス投資 {SelectedBossInvestment()}c。");
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
            ? "整合性違反検知によりラウンド報酬凍結"
            : won
                ? $"勝利報酬 +{WinRewardCredits}c"
                : $"敗北補償 +{LossRewardCredits}c";

        if (!integrityLocked)
        {
            _credits += won ? WinRewardCredits : LossRewardCredits;

            if (bossPayout.TotalInvestedCredits > 0)
            {
                if (bossPayout.InvestmentReturned)
                {
                    _credits += bossPayout.TotalReturnedCredits;
                    economySummary += $" / 投資返還 +{bossPayout.TotalReturnedCredits}c";
                    PushActivityFeed($"投資返還配分: {FormatBossReturnAllocation(bossPayout)}");
                }
                else
                {
                    economySummary += $" / {bossPayout.Reason}";
                }
            }
        }

        var resultSummary = $"{outcomeSummary ?? (won ? "ラウンド勝利。" : "ラウンド敗北。")} {economySummary}。";

        if (HasMatchWinner())
        {
            _resultDestination = won ? GamePhase.Victory : GamePhase.Defeat;
            AwardMatchProgression(won);
            SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。{_lastProgressionSummary}");
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
                SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。第4ラウンド終了につき攻守交代、再エディットを開始します。");
            }
            else
            {
                _resultDestination = GamePhase.Bet;
                if (enteredOvertime)
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。6-6 のためオーバータイムに突入します。");
                }
                else
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。");
                }
            }
        }

        ResetAgentRuntimeState(clearWorldEffects: true);
        _phase = GamePhase.RoundResult;
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
        SetResultMessage($"第{_currentRound}ラウンド準備。{PlayerRoleLabel()}としてボス、総投資額、武器を決めてください。");
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

        SetResultMessage("陣地構築は一度だけ。第1-4ラウンドは防衛、第5ラウンド以降は攻撃へ切り替わります。ボス投資は 300 円付近が最効率です。");

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
        _resultDestination = GamePhase.Bet;
        _showBriefing = false;
        ArmIntegrityGrace();
        if (string.IsNullOrWhiteSpace(_resultMessage))
        {
            SetResultMessage("攻守交代。再エディットで後半戦の配置を調整してください。");
        }
    }

}
