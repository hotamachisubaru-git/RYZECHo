namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    public void UpdateConstructPhase(InputSnapshot input)
    {
        if (input.Press1)
        {
            _selectedBuildTool = BuildToolKind.BlastDoor;
        }
        else if (input.Press2)
        {
            _selectedBuildTool = BuildToolKind.HoneyTrap;
        }
        else if (input.Press3)
        {
            _selectedBuildTool = BuildToolKind.StaticNest;
        }
        else if (input.Press4)
        {
            _selectedBuildTool = BuildToolKind.ReconBeacon;
        }
        else if (input.Press5)
        {
            _selectedBuildTool = BuildToolKind.ShieldRelay;
        }

        if (input.PressQ)
        {
            CycleStructureSkin(-1);
        }
        else if (input.PressE)
        {
            CycleStructureSkin(1);
        }

        if (input.PressR)
        {
            CycleAdTheme();
        }

        if (input.Confirm)
        {
            _sideSwapConstructPending = false;
            BeginBetPhase();
        }
    }

    public void UpdateBetPhase(InputSnapshot input)
    {
        EnsureFriendlyEconomyState();

        if (input.Press1)
        {
            TrySelectBoss("あなた");
        }
        else if (input.Press2)
        {
            TrySelectBoss("北アンカー");
        }
        else if (input.Press3)
        {
            TrySelectBoss("南アンカー");
        }
        else if (input.Press4)
        {
            TrySelectBoss("中央リンク");
        }

        if (input.PressQ)
        {
            CycleLoadoutWeapon(-1);
        }
        else if (input.PressE)
        {
            CycleLoadoutWeapon(1);
        }

        if (input.PressR)
        {
            ToggleLoadoutFocus();
        }

        if (input.AdjustBetLeft)
        {
            AdjustSelectedInvestment(-25);
        }
        else if (input.AdjustBetRight)
        {
            AdjustSelectedInvestment(25);
        }

        SyncSelectedBetTotal();

        if (input.Confirm)
        {
            StartRound();
        }
    }

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

    private void UpdatePlayer(float deltaSeconds, InputSnapshot input)
    {
        if (!_player.IsAlive)
        {
            UpdatePlayerIdleState(deltaSeconds, true);
            return;
        }

        if (input.PressQ)
        {
            _player.Weapon = _playerPrimaryWeapon;
        }
        else if (input.PressE)
        {
            _player.Weapon = _playerSidearmWeapon;
        }

        UpdateShieldRegen(_player, deltaSeconds);
        var weapon = _weaponStats[_player.Weapon];
        var worldMousePosition = ScreenToWorldPoint(input.MousePosition);
        var movement = PointF.Empty;
        var acted = false;

        if (input.MoveUp)
        {
            movement.Y -= 1f;
        }

        if (input.MoveDown)
        {
            movement.Y += 1f;
        }

        if (input.MoveLeft)
        {
            movement.X -= 1f;
        }

        if (input.MoveRight)
        {
            movement.X += 1f;
        }

        if (movement != PointF.Empty)
        {
            var length = MathF.Sqrt((movement.X * movement.X) + (movement.Y * movement.Y));
            movement = new PointF(movement.X / length, movement.Y / length);

            var next = new PointF(
                _player.Position.X + (movement.X * weapon.MoveSpeed * GetActorMoveSpeedMultiplier(_player) * deltaSeconds),
                _player.Position.Y + (movement.Y * weapon.MoveSpeed * GetActorMoveSpeedMultiplier(_player) * deltaSeconds));

            _player.Position = ResolveCollision(next, _player.Radius);
            acted = true;
        }

        _player.FireCooldown = MathF.Max(0f, _player.FireCooldown - deltaSeconds);

        var aimVector = new PointF(worldMousePosition.X - _player.Position.X, worldMousePosition.Y - _player.Position.Y);
        if (Math.Abs(aimVector.X) > 0.01f || Math.Abs(aimVector.Y) > 0.01f)
        {
            _player.FacingAngle = MathF.Atan2(aimVector.Y, aimVector.X);
        }

        if (input.FireHeld && _player.FireCooldown <= 0f)
        {
            var target = PickRaycastTarget(_player.Position, worldMousePosition, weapon.ProjectileRange);
            if (target is not null)
            {
                ApplyDamage(target, weapon.Damage, _player);
            }

            _player.FireCooldown = GetActorFireCooldown(_player, weapon.FireCooldown);
            acted = true;
        }

        if (input.InteractHeld && IsInsideBombSite(_player.Position, 10f))
        {
            acted = true;
        }

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && PlayerHasDirectSightTo(actor.Position)))
        {
            RevealEnemyToTeam(enemy);
        }

        UpdatePlayerIdleState(deltaSeconds, acted);
    }

    private void UpdateAllies(float deltaSeconds)
    {
        foreach (var ally in _allies)
        {
            if (!ally.IsAlive)
            {
                continue;
            }

            UpdateShieldRegen(ally, deltaSeconds);
            ally.FireCooldown = MathF.Max(0f, ally.FireCooldown - deltaSeconds);
            var weapon = _weaponStats[ally.Weapon];
            var target = PickBestTarget(ally.Position, weapon.VisionRange, ActorType.Ally);

            if (target is null)
            {
                if (IsPlayerTeamAttacking())
                {
                    UpdateAttackingAllyMovement(ally, deltaSeconds);
                }

                continue;
            }

            ally.Path.Clear();
            ally.FacingAngle = MathF.Atan2(target.Position.Y - ally.Position.Y, target.Position.X - ally.Position.X);

            if (ally.FireCooldown <= 0f)
            {
                ApplyDamage(target, weapon.Damage, ally);
                ally.FireCooldown = GetActorFireCooldown(ally, weapon.FireCooldown);
                EmitRipple(ally.Position, 0.95f, RippleKind.Gunshot, Color.FromArgb(235, 85, 80));
            }

            if (_pingCooldown <= 0f)
            {
                EmitRipple(target.Position, 0.7f, RippleKind.Skill, Color.FromArgb(245, 208, 96));
                _pingCooldown = 0.7f;
            }
        }
    }

    private void UpdateEnemies(float deltaSeconds)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            UpdateShieldRegen(enemy, deltaSeconds);
            var weapon = _weaponStats[enemy.Weapon];
            enemy.FireCooldown = MathF.Max(0f, enemy.FireCooldown - deltaSeconds);
            enemy.PathCooldown -= deltaSeconds;
            enemy.FootstepCooldown -= deltaSeconds;

            var target = PickEnemyTarget(enemy);

            if (target is not null && Distance(enemy.Position, target.Position) <= weapon.ProjectileRange && HasLineOfSight(enemy.Position, target.Position))
            {
                enemy.Path.Clear();
                enemy.FacingAngle = MathF.Atan2(target.Position.Y - enemy.Position.Y, target.Position.X - enemy.Position.X);
                if (enemy.FireCooldown <= 0f)
                {
                    ApplyDamage(target, weapon.Damage, enemy);
                    enemy.FireCooldown = GetActorFireCooldown(enemy, weapon.FireCooldown * 1.2f);
                    EmitRipple(enemy.Position, 1f, RippleKind.Gunshot, Color.FromArgb(255, 108, 82));
                }
            }
            else if (enemy.PathCooldown <= 0f)
            {
                RebuildEnemyPath(enemy);
            }

            if (EnemyTeamAttacking() && !_bombPlanted && IsInsideBombSite(enemy.Position, _attackFocusSite, 10f))
            {
                enemy.Path.Clear();
                enemy.FacingAngle = MathF.Atan2(BombSitePosition(_attackFocusSite).Y - enemy.Position.Y, BombSitePosition(_attackFocusSite).X - enemy.Position.X);
                continue;
            }

            FollowPathActor(enemy, deltaSeconds, true);
        }
    }

    private void UpdateStructures(float deltaSeconds)
    {
        foreach (var structure in _structures)
        {
            if (structure.Kind is not StructureKind.StaticNest and not StructureKind.ReconBeacon and not StructureKind.ShieldRelay)
            {
                continue;
            }

            structure.PulseCooldown -= deltaSeconds;
            if (structure.PulseCooldown <= 0f)
            {
                switch (structure.Kind)
                {
                    case StructureKind.StaticNest:
                        structure.PulseCooldown = 1.05f;
                        EmitRipple(CellCenter(structure.Cell), 0.72f, RippleKind.Skill, Color.FromArgb(236, 212, 98));
                        break;
                    case StructureKind.ReconBeacon:
                        structure.PulseCooldown = 1.2f;
                        EmitRipple(CellCenter(structure.Cell), 0.82f, RippleKind.Skill, Color.FromArgb(124, 228, 255));
                        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, CellCenter(structure.Cell)) <= 150f))
                        {
                            RevealEnemyToTeam(enemy, SharedVisionDurationSeconds + 0.8f);
                        }

                        break;
                    case StructureKind.ShieldRelay:
                        structure.PulseCooldown = 1.5f;
                        EmitRipple(CellCenter(structure.Cell), 0.68f, RippleKind.Skill, Color.FromArgb(124, 255, 204));
                        foreach (var ally in LivePlayerTeam().Where(actor => Distance(actor.Position, CellCenter(structure.Cell)) <= 130f))
                        {
                            ally.Shield = MathF.Min(ally.MaxShield, ally.Shield + 6f);
                        }

                        break;
                }
            }
        }

        foreach (var door in _structures.Where(structure => structure.Kind == StructureKind.BlastDoor).ToList())
        {
            var doorCenter = CellCenter(door.Cell);

            foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, doorCenter) <= 30f))
            {
                door.Health = MathF.Max(0f, door.Health - (17f * deltaSeconds));
                enemy.Path.Clear();
                EmitRipple(doorCenter, 0.68f, RippleKind.Skill, Color.FromArgb(245, 198, 92));
            }

            if (door.Health <= 0f)
            {
                _structures.Remove(door);
            }
        }
    }

    private void UpdateRipples(float deltaSeconds)
    {
        for (var index = _ripples.Count - 1; index >= 0; index--)
        {
            _ripples[index].Age += deltaSeconds;
            if (_ripples[index].Age >= _ripples[index].Lifetime)
            {
                _ripples.RemoveAt(index);
            }
        }
    }

}
