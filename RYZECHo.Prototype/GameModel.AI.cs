namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void RestoreBossFlags()
    {
        _player.IsBoss = _selectedBossName == _player.Name;
        foreach (var ally in _allies)
        {
            ally.IsBoss = ally.Name == _selectedBossName;
        }
    }

    private string[] BossCandidateNames()
    {
        return [_player.Name, .. _allies.Select(actor => actor.Name)];
    }

    private int GetBossSelectionCount(string actorName)
    {
        return _bossSelectionCounts.TryGetValue(actorName, out var count) ? count : 0;
    }

    private bool AllBossSelectionsSpent()
    {
        return BossCandidateNames().All(name => GetBossSelectionCount(name) >= MaxBossSelectionsPerActor);
    }

    private bool CanSelectBoss(string actorName)
    {
        return AllBossSelectionsSpent() || GetBossSelectionCount(actorName) < MaxBossSelectionsPerActor;
    }

    private int BossSelectionsRemaining(string actorName)
    {
        if (AllBossSelectionsSpent())
        {
            return 1;
        }

        return Math.Max(0, MaxBossSelectionsPerActor - GetBossSelectionCount(actorName));
    }

    private bool TrySelectBoss(string actorName)
    {
        if (CanSelectBoss(actorName))
        {
            _selectedBossName = actorName;
            return true;
        }

        var fallback = BossCandidateNames().FirstOrDefault(CanSelectBoss);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _selectedBossName = fallback;
            SetResultMessage($"{actorName} は既に 2 回選出済みのため、{fallback} に切り替えました。");
            return false;
        }

        _selectedBossName = actorName;
        return true;
    }

    private void EnsureBossSelectionAvailable()
    {
        if (CanSelectBoss(_selectedBossName))
        {
            return;
        }

        var fallback = BossCandidateNames().FirstOrDefault(CanSelectBoss);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _selectedBossName = fallback;
        }
    }

    private float BossInvestmentCoreFactor(int investment)
    {
        if (investment <= 0)
        {
            return 0f;
        }

        var normalized = Math.Clamp(Math.Min(investment, OptimalBossInvestment) / (float)OptimalBossInvestment, 0f, 1f);
        var quadraticPeak = (2f * normalized) - (normalized * normalized);
        if (investment <= OptimalBossInvestment)
        {
            return quadraticPeak;
        }

        var overflow = investment - OptimalBossInvestment;
        var tail = 0.22f * (1f - MathF.Exp(-overflow / 180f));
        return quadraticPeak + tail;
    }

    private float BossMoveBonusPercent(int investment)
    {
        return BossInvestmentCoreFactor(investment) * 0.12f;
    }

    private float BossReloadBonusPercent(int investment)
    {
        return BossInvestmentCoreFactor(investment) * 0.18f;
    }

    private string BossBuffSummary(int investment)
    {
        return $"移動 +{BossMoveBonusPercent(investment) * 100f:0}% / 射撃 +{BossReloadBonusPercent(investment) * 100f:0}%";
    }

    private float BossInvestmentProgress(int investment)
    {
        return Math.Clamp(BossInvestmentCoreFactor(investment) / 1.22f, 0f, 1f);
    }

    private int CurrentBossInvestment(Actor actor)
    {
        if (!actor.IsBoss)
        {
            return 0;
        }

        return actor.Type == ActorType.Enemy ? _enemyBossInvestment : GetFriendlyInvestment(actor.Name);
    }

    private float GetActorMoveSpeedMultiplier(Actor actor)
    {
        return 1f + BossMoveBonusPercent(CurrentBossInvestment(actor));
    }

    private float GetActorFireCooldown(Actor actor, float baseCooldown)
    {
        var fireRateBonus = 1f + BossReloadBonusPercent(CurrentBossInvestment(actor));
        return baseCooldown / Math.Max(1f, fireRateBonus);
    }

    private static bool IsCloseRangeWeapon(WeaponType weapon)
    {
        return weapon is WeaponType.Blitz or WeaponType.Monster or WeaponType.Melt;
    }

    private static bool IsMidRangeWeapon(WeaponType weapon)
    {
        return weapon is WeaponType.Fairy or WeaponType.Giant or WeaponType.Juggernaut;
    }

    private int AffordableCredits()
    {
        return Math.Max(0, _credits - _weaponStats[_selectedWeapon].Cost - _weaponStats[_selectedSidearmWeapon].Cost);
    }

    private Actor? SelectedBoss()
    {
        if (_selectedBossName == _player.Name)
        {
            return _player;
        }

        return _allies.FirstOrDefault(actor => actor.Name == _selectedBossName);
    }

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
        _enemyBossInvestment = Math.Clamp(180 + (_enemyRoundWins * 35) + (_currentRound * 20) + _random.Next(-40, 61), 120, 420);

        for (var index = 0; index < enemyCells.Length; index++)
        {
            var enemy = CreateEnemyActor($"{(EnemyTeamAttacking() ? "襲撃者" : "守備者")}-{index + 1}", enemyCells[index], loadout[index]);
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

    private Actor CreateEnemyActor(string name, Point spawnCell, WeaponType weapon)
    {
        var stats = _weaponStats[weapon];
        var enemyHealth = IsCloseRangeWeapon(weapon) ? 52f : IsMidRangeWeapon(weapon) ? 60f : 48f;
        var enemyShield = IsCloseRangeWeapon(weapon) ? 18f : IsMidRangeWeapon(weapon) ? 26f : 16f;
        return new Actor
        {
            Name = name,
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
                .Where(structure => structure.Kind == StructureKind.BlastDoor)
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
            "北アンカー" => new[] { new Point(siteCell.X + (direction * 2), siteCell.Y - 2), new Point(siteCell.X + (direction * 3), siteCell.Y - 1) },
            "南アンカー" => new[] { new Point(siteCell.X + (direction * 2), siteCell.Y + 2), new Point(siteCell.X + (direction * 3), siteCell.Y + 1) },
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

    private Actor? PickBestTarget(PointF origin, float range, ActorType sourceType)
    {
        IEnumerable<Actor> candidates = sourceType == ActorType.Enemy
            ? LivePlayerTeam()
            : _enemies.Where(actor => actor.IsAlive);

        var target = candidates
            .Where(actor => Distance(origin, actor.Position) <= range && HasLineOfSight(origin, actor.Position))
            .OrderBy(actor => Distance(origin, actor.Position))
            .FirstOrDefault();

        if (target is not null && sourceType != ActorType.Enemy && target.Type == ActorType.Enemy)
        {
            RevealEnemyToTeam(target);
        }

        return target;
    }

    private Actor? PickEnemyTarget(Actor enemy)
    {
        var defenders = LivePlayerTeam()
            .Where(actor => Distance(enemy.Position, actor.Position) <= _weaponStats[enemy.Weapon].ProjectileRange + 30f)
            .OrderBy(actor => Distance(enemy.Position, actor.Position))
            .ToList();

        return defenders.FirstOrDefault(actor => HasLineOfSight(enemy.Position, actor.Position));
    }

    private Actor? PickRaycastTarget(PointF origin, PointF targetPoint, float range)
    {
        var direction = new PointF(targetPoint.X - origin.X, targetPoint.Y - origin.Y);
        var length = MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        if (length <= 1f)
        {
            return null;
        }

        direction = new PointF(direction.X / length, direction.Y / length);

        var bestDistance = float.MaxValue;
        Actor? best = null;

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive))
        {
            var toEnemy = new PointF(enemy.Position.X - origin.X, enemy.Position.Y - origin.Y);
            var projection = (toEnemy.X * direction.X) + (toEnemy.Y * direction.Y);
            if (projection < 0f || projection > range)
            {
                continue;
            }

            var closest = new PointF(origin.X + (direction.X * projection), origin.Y + (direction.Y * projection));
            if (Distance(closest, enemy.Position) <= enemy.Radius + 5f && projection < bestDistance && HasLineOfSight(origin, enemy.Position))
            {
                best = enemy;
                bestDistance = projection;
            }
        }

        return best;
    }

    private bool PlayerHasDirectSightTo(PointF position)
    {
        if (!_player.IsAlive)
        {
            return false;
        }

        var weapon = _weaponStats[_player.Weapon];
        var vector = new PointF(position.X - _player.Position.X, position.Y - _player.Position.Y);
        var distance = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        if (distance > weapon.VisionRange)
        {
            return false;
        }

        if (distance <= 1f)
        {
            return true;
        }

        var angle = MathF.Atan2(vector.Y, vector.X);
        var difference = NormalizeAngle(angle - _player.FacingAngle);
        if (MathF.Abs(difference) > DegreesToRadians(GetFovDegrees(_player.Weapon) / 2f))
        {
            return false;
        }

        return HasLineOfSight(_player.Position, position);
    }

    private bool PlayerCanSee(Actor enemy)
    {
        if (IsEnemySharedVisible(enemy))
        {
            return true;
        }

        if (!PlayerHasDirectSightTo(enemy.Position))
        {
            return false;
        }

        if (_structures.Any(structure => structure.Kind == StructureKind.StaticNest && Distance(enemy.Position, CellCenter(structure.Cell)) <= 90f))
        {
            return Distance(_player.Position, enemy.Position) <= 120f;
        }

        return true;
    }

    private float GetFovDegrees(WeaponType weaponType)
    {
        return _weaponStats[weaponType].ScopedFov ? SniperFovDegrees : DefaultFovDegrees;
    }

    private float GetFootstepInterval(float movementSpeed)
    {
        var normalized = Math.Clamp((movementSpeed - 60f) / 170f, 0f, 1f);
        return 0.42f - (normalized * 0.16f);
    }

    private float GetSoundRangeMultiplier(PointF position)
    {
        return MathF.Pow(0.9f, CountOccludingCells(_player.Position, position));
    }

    private float GetSoundAlphaMultiplier(PointF position)
    {
        return MathF.Pow(0.72f, CountOccludingCells(_player.Position, position));
    }

    private bool PlayerCanPerceive(PointF position, float strength)
    {
        if (_phase != GamePhase.Hunt)
        {
            return true;
        }

        var hearing = _player.HearingRange * _weaponStats[_player.Weapon].HearingMultiplier * 1.8f * strength * GetSoundRangeMultiplier(position);
        return Distance(_player.Position, position) <= hearing;
    }

    private List<Point> FindPath(Point start, Point goal)
    {
        if (start == goal)
        {
            return [start];
        }

        var frontier = new Queue<Point>();
        frontier.Enqueue(start);

        var cameFrom = new Dictionary<Point, Point?>
        {
            [start] = null,
        };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var neighbor in Neighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor) || IsBlockedCell(neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                if (neighbor == goal)
                {
                    frontier.Clear();
                    break;
                }

                frontier.Enqueue(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            return [];
        }

        var path = new List<Point>();
        Point? cursor = goal;
        while (cursor is not null)
        {
            path.Add(cursor.Value);
            cursor = cameFrom[cursor.Value];
        }

        path.Reverse();
        return path;
    }

    private IEnumerable<Point> Neighbors(Point cell)
    {
        var candidates = new[]
        {
            new Point(cell.X + 1, cell.Y),
            new Point(cell.X - 1, cell.Y),
            new Point(cell.X, cell.Y + 1),
            new Point(cell.X, cell.Y - 1),
        };

        foreach (var candidate in candidates)
        {
            if (candidate.X >= 0 && candidate.X < GridColumns && candidate.Y >= 0 && candidate.Y < GridRows)
            {
                yield return candidate;
            }
        }
    }

    private bool IsBlockedCell(Point cell)
    {
        if (_permanentWalls.Contains(cell))
        {
            return true;
        }

        return _structures.Any(structure => structure.Kind == StructureKind.BlastDoor && structure.Cell == cell && structure.Health > 0f);
    }

    private PointF ResolveCollision(PointF desiredPosition, float radius)
    {
        var clamped = new PointF(
            Math.Clamp(desiredPosition.X, WorldBounds.Left + radius + 2f, WorldBounds.Right - radius - 2f),
            Math.Clamp(desiredPosition.Y, WorldBounds.Top + radius + 2f, WorldBounds.Bottom - radius - 2f));

        foreach (var blockedCell in _permanentWalls.Concat(_structures.Where(structure => structure.Kind == StructureKind.BlastDoor && structure.Health > 0f).Select(structure => structure.Cell)))
        {
            var expanded = RectangleF.Inflate(CellRectangle(blockedCell), radius, radius);
            if (!expanded.Contains(clamped))
            {
                continue;
            }

            var center = CellCenter(blockedCell);
            var push = new PointF(clamped.X - center.X, clamped.Y - center.Y);
            var length = MathF.Max(1f, MathF.Sqrt((push.X * push.X) + (push.Y * push.Y)));
            clamped = new PointF(center.X + ((push.X / length) * (CellSize / 2f + radius + 2f)), center.Y + ((push.Y / length) * (CellSize / 2f + radius + 2f)));
        }

        return clamped;
    }

    private bool HasLineOfSight(PointF start, PointF end)
    {
        var distance = Distance(start, end);
        var steps = Math.Max(2, (int)(distance / 8f));

        for (var step = 1; step < steps; step++)
        {
            var progress = step / (float)steps;
            var sample = new PointF(
                start.X + ((end.X - start.X) * progress),
                start.Y + ((end.Y - start.Y) * progress));
            var cell = WorldToCell(sample);
            if (IsBlockedCell(cell))
            {
                return false;
            }
        }

        return true;
    }

    private int CountOccludingCells(PointF start, PointF end)
    {
        var distance = Distance(start, end);
        var steps = Math.Max(2, (int)(distance / 6f));
        var blockedCells = new HashSet<Point>();

        for (var step = 1; step < steps; step++)
        {
            var progress = step / (float)steps;
            var sample = new PointF(
                start.X + ((end.X - start.X) * progress),
                start.Y + ((end.Y - start.Y) * progress));
            var cell = WorldToCell(sample);
            if (IsBlockedCell(cell))
            {
                blockedCells.Add(cell);
            }
        }

        return blockedCells.Count;
    }

    private IEnumerable<Actor> LivePlayerTeam()
    {
        if (_player.IsAlive)
        {
            yield return _player;
        }

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            yield return ally;
        }
    }

    private int LiveEnemyCount()
    {
        return _enemies.Count(enemy => enemy.IsAlive);
    }

    private bool IsPlayerTeamAttacking()
    {
        return _playerTeamRole == TeamRole.Attack;
    }

    private bool EnemyTeamAttacking()
    {
        return !IsPlayerTeamAttacking();
    }

    private static TeamRole ToggleRole(TeamRole role)
    {
        return role == TeamRole.Attack ? TeamRole.Defense : TeamRole.Attack;
    }

    private bool HasMatchWinner()
    {
        return IsWinningScore(_playerRoundWins, _enemyRoundWins) || IsWinningScore(_enemyRoundWins, _playerRoundWins);
    }

    private static bool IsWinningScore(int score, int opponentScore)
    {
        if (score < RoundsToWin)
        {
            return false;
        }

        if (score >= OvertimeTriggerScore && opponentScore >= OvertimeTriggerScore)
        {
            return score - opponentScore >= 2;
        }

        return true;
    }

    private int CurrentAttackerCount()
    {
        return IsPlayerTeamAttacking() ? LivePlayerTeam().Count() : LiveEnemyCount();
    }

    private int CurrentDefenderCount()
    {
        return IsPlayerTeamAttacking() ? LiveEnemyCount() : LivePlayerTeam().Count();
    }

    private string PlayerRoleLabel()
    {
        return IsPlayerTeamAttacking() ? "攻撃側" : "防衛側";
    }

    private string PlayerRoleShortLabel()
    {
        return IsPlayerTeamAttacking() ? "攻撃" : "防衛";
    }

    private int AttackApproachDirection()
    {
        return GetBombSite(_bombPlanted && _armedBombSiteId is not null ? _armedBombSiteId.Value : _attackFocusSite).Cell.X < GridColumns / 2 ? 1 : -1;
    }

    private Point[] GetEnemySetupCells()
    {
        if (EnemyTeamAttacking())
        {
            return _spawnCells.ToArray();
        }

        return new[]
        {
            MirrorCellHorizontally(_player.HomeCell),
            MirrorCellHorizontally(_allies[0].HomeCell),
            MirrorCellHorizontally(_allies[1].HomeCell),
            MirrorCellHorizontally(_allies[2].HomeCell),
        };
    }

}
