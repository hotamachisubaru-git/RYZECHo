namespace RYZECHo;

internal sealed partial class GameModel
{
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
