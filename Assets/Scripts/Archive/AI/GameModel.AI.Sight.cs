namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Sight — 視認・FOV・ラインオブサイト
    // =========================================================================

    private bool PlayerHasDirectSightTo(PointF position)
    {
        return ActorHasDirectSightTo(_player, position);
    }

    private bool ActorHasDirectSightTo(Actor actor, PointF position)
    {
        if (!actor.IsAlive)
        {
            return false;
        }

        var weapon = _weaponStats[actor.Weapon];
        var vector = new PointF(position.X - actor.Position.X, position.Y - actor.Position.Y);
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
        var difference = NormalizeAngle(angle - actor.FacingAngle);
        if (MathF.Abs(difference) > DegreesToRadians(GetFovDegrees(actor.Weapon) / 2f))
        {
            return false;
        }

        return HasLineOfSight(actor, position);
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
            if (_hunterEyeTimer > 0f && PlayerHasDirectSightTo(enemy.Position))
            {
                return true;
            }

            return Distance(_player.Position, enemy.Position) <= 120f;
        }

        return true;
    }

    private float GetFovDegrees(WeaponType weaponType)
    {
        return _weaponStats[weaponType].ScopedFov ? SniperFovDegrees : DefaultFovDegrees;
    }

    private bool HasLineOfSight(Actor actor, PointF end)
    {
        return HasLineOfSight(actor.Type, actor.Position, end);
    }

    private bool HasLineOfSight(ActorType sourceType, PointF start, PointF end)
    {
        if (IsLineBlockedByWorldEffect(start, end))
        {
            return false;
        }

        var distance = Distance(start, end);
        var steps = Math.Max(2, (int)(distance / 8f));

        for (var step = 1; step < steps; step++)
        {
            var progress = step / (float)steps;
            var sample = new PointF(
                start.X + ((end.X - start.X) * progress),
                start.Y + ((end.Y - start.Y) * progress));
            var cell = WorldToCell(sample);
            if (IsVisionBlockedCell(cell, sourceType))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsVisionBlockedCell(Point cell, ActorType sourceType)
    {
        if (_permanentWalls.Contains(cell))
        {
            return true;
        }

        foreach (var structure in _structures.Where(structure => structure.Cell == cell && structure.Health > 0f))
        {
            if (structure.Kind is StructureKind.BlastDoor or StructureKind.PortableCover)
            {
                return true;
            }

            if (structure.Kind == StructureKind.VisorWall && !SameTeamSide(sourceType, structure.OwnerType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SameTeamSide(ActorType left, ActorType right)
    {
        return IsFriendlyActorType(left) == IsFriendlyActorType(right);
    }

    private static bool IsFriendlyActorType(ActorType actorType)
    {
        return actorType is ActorType.Player or ActorType.Ally;
    }

    // =========================================================================
    // Audio Occlusion — 音の遮蔽判定
    // =========================================================================

    private AudioOcclusionProfile GetAudioOcclusionProfile(PointF position)
    {
        return GetAudioOcclusionProfile(_player.Position, position);
    }

    private AudioOcclusionProfile GetAudioOcclusionProfile(PointF listenerPosition, PointF sourcePosition)
    {
        return AudioRippleVisualRules.CalculateOcclusion(CountOccludingCells(listenerPosition, sourcePosition));
    }

    private bool PlayerCanPerceive(PointF position, float strength)
    {
        if (_phase != GamePhase.Hunt)
        {
            return true;
        }

        var hearing = AudioRippleVisualRules.CalculateHearingRange(
            _player.HearingRange,
            _weaponStats[_player.Weapon].HearingMultiplier,
            strength,
            GetAudioOcclusionProfile(position));
        return Distance(_player.Position, position) <= hearing;
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
            if (_permanentWalls.Contains(cell) || _structures.Any(structure => structure.Cell == cell && structure.Health > 0f && structure.Kind is StructureKind.BlastDoor or StructureKind.PortableCover or StructureKind.VisorWall))
            {
                blockedCells.Add(cell);
            }
        }

        return blockedCells.Count;
    }

    // =========================================================================
    // Collision — 衝突回避
    // =========================================================================

    private PointF ResolveCollision(PointF desiredPosition, float radius)
    {
        var clamped = new PointF(
            Math.Clamp(desiredPosition.X, WorldBounds.Left + radius + 2f, WorldBounds.Right - radius - 2f),
            Math.Clamp(desiredPosition.Y, WorldBounds.Top + radius + 2f, WorldBounds.Bottom - radius - 2f));

        foreach (var blockedCell in _permanentWalls.Concat(_structures.Where(structure => IsRouteBlockingStructure(structure.Kind) && structure.Health > 0f).Select(structure => structure.Cell)))
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
}
