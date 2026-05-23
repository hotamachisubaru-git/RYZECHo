namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void CreateEnemySquad()
    {
        var enemyCells = GetEnemySetupCells()
            .OrderBy(_ => _random.Next())
            .Take(TeamSize)
            .ToArray();
        var loadout = EnemyTeamAttacking()
            ? new List<WeaponType> { WeaponType.Blitz, WeaponType.Monster, WeaponType.Fairy, WeaponType.Howl }
            : new List<WeaponType> { WeaponType.Violet, WeaponType.Giant, WeaponType.Fairy, WeaponType.Monster };
        loadout = loadout
            .OrderBy(_ => _random.Next())
            .ToList();
        var agents = AgentCatalog.SelectionOrder
            .OrderBy(_ => _random.Next())
            .Take(TeamSize)
            .ToList();
        _enemyBossInvestment = Math.Clamp(180 + (_enemyRoundWins * 35) + (_currentRound * 20) + _random.Next(-40, 61), 120, 420);

        for (var index = 0; index < enemyCells.Length; index++)
        {
            var enemy = CreateEnemyActor($"{(EnemyTeamAttacking() ? "襲撃者" : "守備者")}-{index + 1}", enemyCells[index], loadout[index], agents[index]);
            _enemies.Add(enemy);
            EmitRipple(enemy.Position, 0.82f, RippleKind.Skill, Color.FromArgb(245, 202, 96));
        }

        var enemyBoss = _enemies
            .OrderByDescending(enemy => _weaponStats[enemy.Weapon].Cost)
            .ThenBy(_ => _random.Next())
            .FirstOrDefault();
        if (enemyBoss is not null)
        {
            enemyBoss.IsBoss = true;
            EmitRipple(enemyBoss.Position, 0.94f, RippleKind.Skill, Color.FromArgb(255, 222, 122));
        }
    }

    private Actor CreateEnemyActor(string name, Point spawnCell, WeaponType weapon, AgentKind agent)
    {
        var stats = _weaponStats[weapon];
        var enemyHealth = IsCloseRangeWeapon(weapon) ? 52f : IsMidRangeWeapon(weapon) ? 60f : 48f;
        var enemyShield = IsCloseRangeWeapon(weapon) ? 18f : IsMidRangeWeapon(weapon) ? 26f : 16f;
        return new Actor
        {
            Name = name,
            Agent = agent,
            Type = ActorType.Enemy,
            HomeCell = spawnCell,
            Weapon = weapon,
            Position = CellCenter(spawnCell),
            Radius = 13f,
            MaxHealth = enemyHealth,
            MaxShield = enemyShield,
            Health = enemyHealth,
            Shield = enemyShield,
            HearingRange = 260f,
            BaseMoveSpeed = stats.MoveSpeed * 0.7f,
        };
    }

    private void RebuildEnemyPath(Actor enemy)
    {
        enemy.Path.Clear();
        enemy.PathCooldown = 0.65f;

        var start = WorldToCell(enemy.Position);
        var desiredGoal = PickPathGoal(enemy);
        var path = FindPath(start, desiredGoal);

        if (path.Count == 0)
        {
            var nearestDoor = _structures
                .Where(structure => IsRouteBlockingStructure(structure.Kind))
                .OrderBy(structure => Distance(enemy.Position, CellCenter(structure.Cell)))
                .FirstOrDefault();

            if (nearestDoor is not null)
            {
                path = FindPath(start, nearestDoor.Cell);
            }
        }

        foreach (var cell in path.Skip(1))
        {
            enemy.Path.Enqueue(CellCenter(cell));
        }
    }

    private Point PickPathGoal(Actor enemy)
    {
        var siteCell = GetBombSite(_bombPlanted && _armedBombSiteId is not null ? _armedBombSiteId.Value : _attackFocusSite).Cell;
        var hostileDecoy = _structures
            .Where(structure => structure.Kind == StructureKind.HoloDecoy && structure.Health > 0f && !SameTeamSide(enemy.Type, structure.OwnerType))
            .Where(structure => Distance(enemy.Position, CellCenter(structure.Cell)) < 260f)
            .OrderBy(structure => Distance(enemy.Position, CellCenter(structure.Cell)))
            .FirstOrDefault();
        if (hostileDecoy is not null && _random.NextDouble() < 0.42)
        {
            return hostileDecoy.Cell;
        }

        var playerTeam = LivePlayerTeam()
            .OrderBy(actor => Distance(enemy.Position, actor.Position))
            .ToList();

        if (EnemyTeamAttacking())
        {
            if (!_bombPlanted)
            {
                if (IsInsideBombSite(enemy.Position, 10f))
                {
                    return siteCell;
                }

                if (playerTeam.Count > 0 && Distance(enemy.Position, playerTeam[0].Position) < 180f)
                {
                    return WorldToCell(playerTeam[0].Position);
                }

                return siteCell;
            }

            if (playerTeam.Count > 0 && Distance(enemy.Position, playerTeam[0].Position) < 180f)
            {
                return WorldToCell(playerTeam[0].Position);
            }

            return siteCell;
        }

        if (_bombPlanted)
        {
            return siteCell;
        }

        if (IsPlayerBreathingExposed() && _player.IsAlive && Distance(enemy.Position, _player.Position) < 150f)
        {
            return WorldToCell(_player.Position);
        }

        if (playerTeam.Count > 0 && Distance(enemy.Position, playerTeam[0].Position) < 180f)
        {
            return WorldToCell(playerTeam[0].Position);
        }

        return enemy.HomeCell;
    }

    private void UpdateAttackingAllyMovement(Actor ally, float deltaSeconds)
    {
        ally.PathCooldown -= deltaSeconds;
        if (ally.PathCooldown <= 0f || ally.Path.Count == 0)
        {
            RebuildAllyPath(ally);
        }

        FollowPathActor(ally, deltaSeconds, false);
    }

    private void RebuildAllyPath(Actor ally)
    {
        ally.Path.Clear();
        ally.PathCooldown = 0.75f;

        var start = WorldToCell(ally.Position);
        var goal = PickAllyAttackGoal(ally);
        var path = FindPath(start, goal);
        if (path.Count == 0)
        {
            path = FindPath(start, WorldToCell(_player.Position));
        }

        foreach (var cell in path.Skip(1))
        {
            ally.Path.Enqueue(CellCenter(cell));
        }
    }

    private Point PickAllyAttackGoal(Actor ally)
    {
        var siteCell = GetBombSite(_bombPlanted && _armedBombSiteId is not null ? _armedBombSiteId.Value : _attackFocusSite).Cell;
        var direction = AttackApproachDirection();
        var candidates = ally.Name switch
        {
            RosterCatalog.NorthAnchorName => new[] { new Point(siteCell.X + (direction * 2), siteCell.Y - 2), new Point(siteCell.X + (direction * 3), siteCell.Y - 1) },
            RosterCatalog.SouthAnchorName => new[] { new Point(siteCell.X + (direction * 2), siteCell.Y + 2), new Point(siteCell.X + (direction * 3), siteCell.Y + 1) },
            _ => new[] { new Point(siteCell.X + (direction * 3), siteCell.Y), new Point(siteCell.X + (direction * 2), siteCell.Y) },
        };

        foreach (var candidate in candidates)
        {
            if (candidate.X < 1 || candidate.X >= GridColumns - 1 || candidate.Y < 1 || candidate.Y >= GridRows - 1)
            {
                continue;
            }

            if (!IsBlockedCell(candidate))
            {
                return candidate;
            }
        }

        return siteCell;
    }

    private void FollowPathActor(Actor actor, float deltaSeconds, bool emitFootsteps)
    {
        if (actor.Path.Count == 0)
        {
            return;
        }

        var waypoint = actor.Path.Peek();
        var vector = new PointF(waypoint.X - actor.Position.X, waypoint.Y - actor.Position.Y);
        var length = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));

        if (length <= 4f)
        {
            actor.Path.Dequeue();
            return;
        }

        vector = new PointF(vector.X / length, vector.Y / length);

        var speed = actor.BaseMoveSpeed * GetActorMoveSpeedMultiplier(actor);
        var cell = WorldToCell(actor.Position);
        if (_structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell))
        {
            speed *= 0.45f;
        }

        var next = new PointF(actor.Position.X + (vector.X * speed * deltaSeconds), actor.Position.Y + (vector.Y * speed * deltaSeconds));
        actor.Position = ResolveCollision(next, actor.Radius);
        actor.FacingAngle = MathF.Atan2(vector.Y, vector.X);

        if (!emitFootsteps || actor.FootstepCooldown > 0f)
        {
            return;
        }

        var stepStrength = _structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell) ? 1.05f : 0.68f;
        actor.FootstepPulseIndex = (actor.FootstepPulseIndex + 1) % 3;
        if (actor.FootstepPulseIndex == 0)
        {
            EmitRipple(actor.Position, stepStrength, RippleKind.Footstep, Color.FromArgb(250, 248, 248, 248));
        }

        actor.FootstepCooldown = GetFootstepInterval(speed);
    }

}
