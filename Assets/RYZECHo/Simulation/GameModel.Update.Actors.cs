namespace RYZECHo;

internal sealed partial class GameModel
{
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
        UpdateActorAbilityState(_player, deltaSeconds);
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

        if (movement != PointF.Empty && !IsActorLockedDown(_player))
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

        if (input.FireHeld && _player.FireCooldown <= 0f && !IsActorSystemCrashed(_player) && !IsActorLockedDown(_player))
        {
            var target = PickRaycastTarget(_player.Position, worldMousePosition, weapon.ProjectileRange);
            if (target is not null)
            {
                ApplyDamage(target, weapon.Damage * PlayerDamageMultiplier(), _player);
            }

            _player.FireCooldown = GetActorFireCooldown(_player, weapon.FireCooldown);
            acted = true;
        }

        if (input.InteractHeld && IsInsideBombSite(_player.Position, 10f))
        {
            acted = true;
        }

        RevealEnemiesInActorVision(_player);

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
            UpdateActorAbilityState(ally, deltaSeconds);
            ally.FireCooldown = MathF.Max(0f, ally.FireCooldown - deltaSeconds);
            var weapon = _weaponStats[ally.Weapon];
            var target = PickBestTarget(ally.Position, weapon.VisionRange, ActorType.Ally);
            TryUseAutonomousAgentAbility(ally, target, deltaSeconds);

            if (IsActorSystemCrashed(ally) || IsActorLockedDown(ally))
            {
                ally.Path.Clear();
                RevealEnemiesInActorVision(ally);
                continue;
            }

            if (target is null)
            {
                if (IsPlayerTeamAttacking())
                {
                    UpdateAttackingAllyMovement(ally, deltaSeconds);
                }

                RevealEnemiesInActorVision(ally);
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

            RevealEnemiesInActorVision(ally);
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
            UpdateActorAbilityState(enemy, deltaSeconds);
            var weapon = _weaponStats[enemy.Weapon];
            enemy.FireCooldown = MathF.Max(0f, enemy.FireCooldown - deltaSeconds);
            enemy.PathCooldown -= deltaSeconds;
            enemy.FootstepCooldown -= deltaSeconds;

            if (IsSystemCrashActive() || IsActorLockedDown(enemy))
            {
                enemy.Path.Clear();
                continue;
            }

            var target = PickEnemyTarget(enemy);
            TryUseAutonomousAgentAbility(enemy, target, deltaSeconds);

            if (target is not null && Distance(enemy.Position, target.Position) <= weapon.ProjectileRange && HasLineOfSight(enemy, target.Position))
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
        for (var index = _structures.Count - 1; index >= 0; index--)
        {
            var structure = _structures[index];
            if (structure.RemainingLifetime <= 0f)
            {
                continue;
            }

            structure.RemainingLifetime -= deltaSeconds;
            if (structure.RemainingLifetime <= 0f)
            {
                _structures.RemoveAt(index);
            }
        }

        foreach (var structure in _structures)
        {
            if (structure.Kind is not StructureKind.StaticNest and not StructureKind.ReconBeacon and not StructureKind.ShieldRelay and not StructureKind.VisorWall and not StructureKind.HoloDecoy)
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
                    case StructureKind.VisorWall:
                        structure.PulseCooldown = 1.8f;
                        EmitRipple(CellCenter(structure.Cell), 0.42f, RippleKind.Skill, Color.FromArgb(150, 150, 228, 255));
                        break;
                    case StructureKind.HoloDecoy:
                        structure.PulseCooldown = 1.15f;
                        EmitRipple(CellCenter(structure.Cell), 0.94f, _random.Next(0, 2) == 0 ? RippleKind.Footstep : RippleKind.Gunshot, Color.FromArgb(226, 196, 132, 255));
                        break;
                }
            }
        }

        foreach (var door in _structures.Where(structure => IsRouteBlockingStructure(structure.Kind)).ToList())
        {
            var doorCenter = CellCenter(door.Cell);

            foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, doorCenter) <= 30f))
            {
                door.Health = MathF.Max(0f, door.Health - (StructureBreakDamagePerSecond(door.Kind) * deltaSeconds));
                enemy.Path.Clear();
                EmitRipple(doorCenter, 0.68f, RippleKind.Skill, Color.FromArgb(245, 198, 92));
            }

            if (door.Health <= 0f)
            {
                _eventBus.Emit(new StructureDestroyedEvent(door.Kind, door.Cell, CellCenter(door.Cell)));
                _structures.Remove(door);
            }
        }
    }

    private static float StructureBreakDamagePerSecond(StructureKind kind)
    {
        return kind switch
        {
            StructureKind.PortableCover => 24f,
            StructureKind.VisorWall => 22f,
            _ => 17f,
        };
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
