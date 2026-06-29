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
}
