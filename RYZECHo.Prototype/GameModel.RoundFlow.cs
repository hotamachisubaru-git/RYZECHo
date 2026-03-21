namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private bool ActiveSelection(WeaponType weaponType)
    {
        return _phase switch
        {
            GamePhase.Bet => SelectedLoadoutWeapon() == weaponType,
            _ => _player.Weapon == weaponType,
        };
    }

    private WeaponType DisplayedWeaponType()
    {
        return _phase == GamePhase.Bet ? SelectedLoadoutWeapon() : _player.Weapon;
    }

    private string WeaponDisplayName(WeaponType weaponType)
    {
        return _weaponStats[weaponType].ShortLabel;
    }

    private string WeaponLoadoutLabel(WeaponType weaponType)
    {
        return _weaponStats[weaponType].Code;
    }

    private int CurrentMagazineAmmo()
    {
        return _weaponStats[DisplayedWeaponType()].MagazineAmmo;
    }

    private int CurrentReserveAmmo()
    {
        return _weaponStats[DisplayedWeaponType()].ReserveAmmo;
    }

    private bool IsActorOnHoneyTrap(Actor actor)
    {
        var cell = WorldToCell(actor.Position);
        return _structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell);
    }

    private bool IsActorInStaticField(Actor actor)
    {
        return _structures.Any(structure => structure.Kind == StructureKind.StaticNest && Distance(actor.Position, CellCenter(structure.Cell)) <= 90f);
    }

    private void ApplyDamage(Actor actor, float damage, Actor? attacker = null)
    {
        if (!actor.IsAlive || damage <= 0f)
        {
            return;
        }

        var wasAlive = actor.IsAlive;
        actor.ShieldRegenDelay = 2.4f;
        if (actor.Shield > 0f)
        {
            var absorbed = MathF.Min(actor.Shield, damage);
            actor.Shield -= absorbed;
            damage -= absorbed;
        }

        if (damage > 0f)
        {
            actor.Health = MathF.Max(0f, actor.Health - damage);
        }

        if (wasAlive && !actor.IsAlive)
        {
            HandleActorEliminated(attacker, actor);
        }
    }

    private static void UpdateShieldRegen(Actor actor, float deltaSeconds)
    {
        if (!actor.IsAlive || actor.MaxShield <= 0f)
        {
            return;
        }

        actor.ShieldRegenDelay = MathF.Max(0f, actor.ShieldRegenDelay - deltaSeconds);
        if (actor.ShieldRegenDelay > 0f || actor.Shield >= actor.MaxShield)
        {
            return;
        }

        actor.Shield = MathF.Min(actor.MaxShield, actor.Shield + (actor.MaxShield * 0.22f * deltaSeconds) + (8f * deltaSeconds));
    }

    private void PushActivityFeed(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_activityFeed.Count == 0 || _activityFeed[0] != message)
        {
            _activityFeed.Insert(0, message);
        }

        if (_activityFeed.Count > 5)
        {
            _activityFeed.RemoveRange(5, _activityFeed.Count - 5);
        }
    }

    private void SetResultMessage(string message)
    {
        _resultMessage = message;
        PushActivityFeed(message);
    }

    private void HandleActorEliminated(Actor? attacker, Actor victim)
    {
        if (attacker is null || ReferenceEquals(attacker, victim))
        {
            PushActivityFeed($"{victim.Name} が離脱。");
            return;
        }

        var playerTeamScoredKill = attacker.Type != ActorType.Enemy;
        if (playerTeamScoredKill)
        {
            _matchTeamEliminations++;
            _credits += KillRewardCredits;
            PushActivityFeed($"{attacker.Name} が {victim.Name} を撃破。+{KillRewardCredits}c。");
            AwardUltPoints(attacker.Name, 1, "撃破");

            if (attacker.IsBoss)
            {
                _roundBossKillCount++;
                var livingAllies = LivePlayerTeam().Count();
                var dividend = livingAllies * BossKillDividendCredits;
                if (dividend > 0)
                {
                    _credits += dividend;
                    PushActivityFeed($"ボス撃破配当。生存中の味方 {livingAllies} 名へ +{BossKillDividendCredits}c、合計 +{dividend}c。");
                }
            }

            if (victim.IsBoss)
            {
                _credits += BossEliminationBonusCredits;
                PushActivityFeed($"敵ボス {victim.Name} を撃破。+{BossEliminationBonusCredits}c を獲得。");
                AwardUltPoints(_selectedBossName, 2, "敵ボス撃破");
            }

            return;
        }

        if (victim.Type == ActorType.Player)
        {
            _matchPlayerDeaths++;
        }

        PushActivityFeed($"{attacker.Name} が {victim.Name} を撃破。");
        if (attacker.IsBoss)
        {
            PushActivityFeed($"敵ボス {attacker.Name} がキルを取得。敵側へ生存味方配当が発生。");
        }

        if (victim.IsBoss)
        {
            PushActivityFeed($"味方ボス {victim.Name} が撃破され、投資は没収。敵は +{BossEliminationBonusCredits}c / ULT+2 相当を獲得。");
        }
    }

    private void TryPlaceStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        if (!_buildSlots.Contains(cell) || _structures.Any(structure => structure.Cell == cell))
        {
            return;
        }

        var candidate = CreateStructure(_selectedBuildTool, cell);
        if (candidate.APCost > _buildPoints)
        {
            return;
        }

        var placementRule = ValidateStructurePlacement(candidate);
        if (placementRule is not null)
        {
            SetResultMessage(placementRule);
            return;
        }

        _buildPoints -= candidate.APCost;
        _structures.Add(candidate);
        SetResultMessage($"{candidate.Label} を {cell.X},{cell.Y} に設置。");
    }

    private void TryRemoveStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        var structure = _structures.FirstOrDefault(candidate => candidate.Cell == cell);
        if (structure is null)
        {
            return;
        }

        _buildPoints += structure.APCost;
        _structures.Remove(structure);
        SetResultMessage($"{structure.Label} を撤去して AP を返還。");
    }

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
        _roundBossKillCount = 0;
        _enemyBossInvestment = 0;
        _playerIdleSeconds = 0f;
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
        }

        foreach (var structure in _structures.Where(structure => structure.Kind == StructureKind.BlastDoor))
        {
            structure.Health = structure.MaxHealth;
        }

        _enemies.Clear();
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
        var bossQualifiedDividend = bossAlive && _roundBossKillCount > 0;
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

            if (_selectedBet > 0)
            {
                if (won && bossQualifiedDividend)
                {
                    var returnCredits = _selectedBet * 2;
                    _credits += returnCredits;
                    economySummary += $" / 投資返還 +{returnCredits}c";
                }
                else if (bossAlive)
                {
                    economySummary += won
                        ? " / ボス無撃破のため投資返還なし"
                        : " / ボス生存も敗北のため投資返還なし";
                }
                else
                {
                    economySummary += " / ボス撃破により投資没収";
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
                _buildPoints = Math.Max(_buildPoints, 12);
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

        _phase = GamePhase.RoundResult;
        _resultTimer = 2.4f;
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
        SetResultMessage($"第{_currentRound}ラウンド準備。{PlayerRoleLabel()}としてボス、総投資額、武器を決めてください。");
    }

    private void ResetCampaign()
    {
        _buildPoints = 12;
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
        _selectedBossName = "あなた";
        _coreHealth = 180f;
        _bombPlanted = false;
        _armedBombSiteId = null;
        _bombPlantProgress = 0f;
        _bombDefuseProgress = 0f;
        _activePlanter = null;
        _isOvertime = false;
        _sideSwapConstructPending = false;
        _playerIdleSeconds = 0f;
        _playerTeamRole = TeamRole.Defense;
        _phase = GamePhase.Construct;
        _resultDestination = GamePhase.Bet;
        _showBriefing = true;
        _enemyBossInvestment = 0;
        _matchTeamEliminations = 0;
        _matchPlayerDeaths = 0;
        _roundBossKillCount = 0;
        _structures.Clear();
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
        _player.PathCooldown = 0f;
        _player.Path.Clear();

        if (_allies.Count >= 3)
        {
            _allies[0].Weapon = WeaponType.Violet;
            _allies[1].Weapon = WeaponType.Blitz;
            _allies[2].Weapon = WeaponType.Fairy;
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
