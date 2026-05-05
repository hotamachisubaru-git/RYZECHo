namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void UpdateHuntPhase(float deltaSeconds, InputSnapshot input)
    {
        _roundTimer -= deltaSeconds;
        _pingCooldown -= deltaSeconds;
        UpdateSharedVision(deltaSeconds);

        RestoreBossFlags();
        UpdatePlayer(deltaSeconds, input);
        UpdateAllies(deltaSeconds);
        UpdateEnemies(deltaSeconds);
        UpdateStructures(deltaSeconds);

        if (IsPlayerTeamAttacking())
        {
            ResolveAttackingRoundState(deltaSeconds, input);
            return;
        }

        ResolveDefendingRoundState(deltaSeconds, input);
    }

    private void ResolveAttackingRoundState(float deltaSeconds, InputSnapshot input)
    {
        if (LiveEnemyCount() == 0)
        {
            EndRound(true, _bombPlanted ? "守備班壊滅。爆破を待てば突破成立です。" : "守備班壊滅。設置前にサイトを制圧しました。");
            return;
        }

        if (!LivePlayerTeam().Any() && !_bombPlanted)
        {
            EndRound(false, "攻撃班壊滅。設置に失敗しました。");
            return;
        }

        if (UpdateBombObjective(deltaSeconds, input))
        {
            return;
        }

        if (_coreHealth <= 0f)
        {
            EndRound(true, "ボム爆破成功。サイトを突破しました。");
            return;
        }

        if (!_bombPlanted && _roundTimer <= 0f)
        {
            EndRound(false, "設置猶予が終了。攻撃失敗です。");
        }
    }

    private void ResolveDefendingRoundState(float deltaSeconds, InputSnapshot input)
    {
        if (!_bombPlanted && LiveEnemyCount() == 0)
        {
            EndRound(true, "襲撃班を排除。設置前に制圧しました。");
            return;
        }

        if (!LivePlayerTeam().Any())
        {
            EndRound(false, _bombPlanted ? "防衛班壊滅。解除できずサイトを失いました。" : "防衛班壊滅。サイト防衛に失敗しました。");
            return;
        }

        if (UpdateBombObjective(deltaSeconds, input))
        {
            return;
        }

        if (_coreHealth <= 0f)
        {
            EndRound(false, "ボムが爆発。サイト防衛に失敗しました。");
            return;
        }

        if (!_bombPlanted && _roundTimer <= 0f)
        {
            EndRound(true, "設置猶予を守り切りました。");
        }
    }

    private bool UpdateBombObjective(float deltaSeconds, InputSnapshot input)
    {
        return IsPlayerTeamAttacking()
            ? UpdateAttackingBombObjective(deltaSeconds, input)
            : UpdateDefendingBombObjective(deltaSeconds, input);
    }

    private bool UpdateAttackingBombObjective(float deltaSeconds, InputSnapshot input)
    {
        if (_bombPlanted)
        {
            UpdateEnemyDefuse(deltaSeconds);

            if (_roundTimer <= 0f)
            {
                _coreHealth = 0f;
                EndRound(true, "ボム爆破成功。サイトを突破しました。");
                return true;
            }

            return false;
        }

        UpdatePlayerTeamPlant(deltaSeconds, input);
        return false;
    }

    private bool UpdateDefendingBombObjective(float deltaSeconds, InputSnapshot input)
    {
        if (_bombPlanted)
        {
            UpdatePlayerTeamDefuse(deltaSeconds, input);

            if (_roundTimer <= 0f)
            {
                _coreHealth = 0f;
                EndRound(false, "ボムが爆発。サイト防衛に失敗しました。");
                return true;
            }

            return false;
        }

        var planter = _enemies
            .Where(enemy => enemy.IsAlive && IsInsideBombSite(enemy.Position, 10f))
            .OrderBy(enemy => Distance(enemy.Position, BombSitePosition(_attackFocusSite)))
            .FirstOrDefault();

        _activePlanter = planter;
        if (planter is null)
        {
            _bombPlantProgress = MathF.Max(0f, _bombPlantProgress - (deltaSeconds * 2.2f));
            return false;
        }

        _bombPlantProgress += deltaSeconds;
        if (_bombPlantProgress < BombPlantSeconds)
        {
            return false;
        }

        var site = TryGetBombSiteAt(planter.Position, out var activeSite, 10f)
            ? activeSite.Id
            : FindClosestSite(planter.Position).Id;
        ArmBomb(planter, false, site);
        return false;
    }

    private void UpdatePlayerTeamPlant(float deltaSeconds, InputSnapshot input)
    {
        Actor? planter = null;
        if (_player.IsAlive && input.InteractHeld && IsInsideBombSite(_player.Position, 10f))
        {
            planter = _player;
        }
        else
        {
            planter = _allies
                .Where(ally => ally.IsAlive && IsInsideBombSite(ally.Position, 10f))
                .OrderBy(ally => Distance(ally.Position, BombSitePosition(_attackFocusSite)))
                .FirstOrDefault();
        }

        _activePlanter = planter;
        if (planter is null)
        {
            _bombPlantProgress = MathF.Max(0f, _bombPlantProgress - (deltaSeconds * 2.2f));
            return;
        }

        _bombPlantProgress += deltaSeconds;
        if (_bombPlantProgress < BombPlantSeconds)
        {
            return;
        }

        var site = TryGetBombSiteAt(planter.Position, out var activeSite, 10f)
            ? activeSite.Id
            : FindClosestSite(planter.Position).Id;
        ArmBomb(planter, true, site);
    }

    private void ArmBomb(Actor planter, bool plantedByPlayerTeam, ObjectiveSiteId siteId)
    {
        _bombPlanted = true;
        _armedBombSiteId = siteId;
        _bombPlantProgress = BombPlantSeconds;
        _bombDefuseProgress = 0f;
        _roundTimer = BombFuseSeconds;
        if (plantedByPlayerTeam)
        {
            _credits += ObjectiveRewardCredits;
            PushActivityFeed($"設置成功。+{ObjectiveRewardCredits}c。");
            AwardUltPoints(_selectedBossName, 1, $"サイト {GetBombSite(siteId).Label} 設置");
        }

        EmitRipple(BombSitePosition(siteId), 0.92f, RippleKind.Skill, Color.FromArgb(245, 208, 96));
        SetResultMessage(plantedByPlayerTeam
            ? $"{planter.Name} がサイト {GetBombSite(siteId).Label} にボムを設置。35 秒守り切ってください。"
            : $"{planter.Name} がサイト {GetBombSite(siteId).Label} にボムを設置。35 秒以内に解除してください。");
    }

    private void UpdatePlayerTeamDefuse(float deltaSeconds, InputSnapshot input)
    {
        var canPlayerDefuse = CanPlayerDefuse() && input.InteractHeld;
        var remoteFailSafe = !_player.IsAlive && LiveEnemyCount() == 0 && LivePlayerTeam().Any();

        if (canPlayerDefuse)
        {
            _bombDefuseProgress = MathF.Min(BombDefuseSeconds, _bombDefuseProgress + deltaSeconds);
        }
        else if (remoteFailSafe)
        {
            _bombDefuseProgress = MathF.Min(BombDefuseSeconds, _bombDefuseProgress + (deltaSeconds * 0.45f));
        }
        else
        {
            _bombDefuseProgress = MathF.Max(0f, _bombDefuseProgress - (deltaSeconds * 2.4f));
        }

        if (_bombDefuseProgress < BombDefuseSeconds)
        {
            return;
        }

        _bombPlanted = false;
        _bombDefuseProgress = BombDefuseSeconds;
        _credits += ObjectiveRewardCredits;
        PushActivityFeed($"解除成功。+{ObjectiveRewardCredits}c。");
        AwardUltPoints(_selectedBossName, 1, "ボム解除");
        var defusedSite = _armedBombSiteId ?? _attackFocusSite;
        _armedBombSiteId = null;
        EmitRipple(BombSitePosition(defusedSite), 0.88f, RippleKind.Skill, Color.FromArgb(120, 228, 208));
        EndRound(true, remoteFailSafe ? "味方班が遠隔停止に成功。ボムを解除しました。" : "ボム解除成功。サイトを守り切りました。");
    }

    private void UpdateEnemyDefuse(float deltaSeconds)
    {
        if (_armedBombSiteId is null)
        {
            return;
        }

        var sitePosition = BombSitePosition(_armedBombSiteId.Value);
        var defuser = _enemies
            .Where(enemy => enemy.IsAlive && IsInsideBombSite(enemy.Position, _armedBombSiteId.Value, 10f))
            .Where(enemy => !LivePlayerTeam().Any(attacker => attacker.IsAlive && Distance(attacker.Position, sitePosition) <= 110f))
            .OrderBy(enemy => Distance(enemy.Position, sitePosition))
            .FirstOrDefault();

        _activePlanter = defuser;
        if (defuser is null)
        {
            _bombDefuseProgress = MathF.Max(0f, _bombDefuseProgress - (deltaSeconds * 2.4f));
            return;
        }

        _bombDefuseProgress = MathF.Min(BombDefuseSeconds, _bombDefuseProgress + deltaSeconds);
        if (_bombDefuseProgress < BombDefuseSeconds)
        {
            return;
        }

        _bombPlanted = false;
        _bombDefuseProgress = BombDefuseSeconds;
        _armedBombSiteId = null;
        EmitRipple(sitePosition, 0.88f, RippleKind.Skill, Color.FromArgb(255, 132, 108));
        EndRound(false, $"{defuser.Name} がボムを解除。攻撃に失敗しました。");
    }

    private void UpdateRoundResult(float deltaSeconds, InputSnapshot input)
    {
        _resultTimer -= deltaSeconds;

        if (input.Confirm || _resultTimer <= 0f)
        {
            if (_resultDestination == GamePhase.Bet)
            {
                BeginBetPhase();
            }
            else if (_resultDestination == GamePhase.Construct)
            {
                EnterSideSwapConstructPhase();
            }
            else
            {
                _phase = _resultDestination;
            }
        }
    }

    private void UpdateEndState(InputSnapshot input)
    {
        if (input.PressR)
        {
            ResetCampaign();
        }
    }

}
