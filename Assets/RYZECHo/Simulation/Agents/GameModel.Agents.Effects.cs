namespace RYZECHo;

internal sealed partial class GameModel
{
    private bool AddWorldEffect(WorldEffectKind kind, PointF position, float radius, float lifetime, Color color, string message)
    {
        return AddWorldEffect(kind, position, radius, lifetime, color, message, ActorType.Player);
    }

    private bool AddWorldEffect(WorldEffectKind kind, PointF position, float radius, float lifetime, Color color, string message, ActorType ownerType)
    {
        _worldEffects.Add(new WorldEffect
        {
            Kind = kind,
            Position = ClampToWorld(position),
            Radius = radius,
            Lifetime = lifetime,
            Color = color,
            OwnerType = ownerType,
        });

        if (!string.IsNullOrWhiteSpace(message))
        {
            SetResultMessage(message);
        }

        return true;
    }

    private bool TryPlaceTemporaryStructure(BuildToolKind tool, PointF target, float lifetime, string message)
    {
        return TryPlaceTemporaryStructure(tool, target, lifetime, message, ActorType.Player);
    }

    private bool TryPlaceTemporaryStructure(BuildToolKind tool, PointF target, float lifetime, string message, ActorType ownerType)
    {
        var cell = WorldToCell(target);
        if (_permanentWalls.Contains(cell) || _structures.Any(structure => structure.Cell == cell) || IsInsideBombSite(CellCenter(cell), 4f))
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetResultMessage("その位置にはスキル設置できません。");
            }

            return false;
        }

        var structure = CreateStructure(tool, cell);
        structure.RemainingLifetime = lifetime;
        structure.Health = structure.MaxHealth;
        structure.OwnerType = ownerType;
        _structures.Add(structure);
        _eventBus.Emit(new StructureBuiltEvent(structure.Kind, cell, CellCenter(cell), ownerType.ToString()));
        if (!string.IsNullOrWhiteSpace(message))
        {
            SetResultMessage(message);
        }

        return true;
    }

    private PointF ClampToWorld(PointF point)
    {
        return new PointF(
            Math.Clamp(point.X, WorldBounds.Left + 4f, WorldBounds.Right - 4f),
            Math.Clamp(point.Y, WorldBounds.Top + 4f, WorldBounds.Bottom - 4f));
    }

    private bool IsPlayerSilenced()
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SilenceZone && SameTeamSide(_player.Type, effect.OwnerType) && Distance(_player.Position, effect.Position) <= effect.Radius);
    }

    private bool IsActorLockedDown(Actor actor)
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.Lockdown && !SameTeamSide(actor.Type, effect.OwnerType) && Distance(actor.Position, effect.Position) <= effect.Radius);
    }

    private bool IsActorSystemCrashed(Actor actor)
    {
        return _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SystemCrash && !SameTeamSide(actor.Type, effect.OwnerType) && Distance(actor.Position, effect.Position) <= effect.Radius);
    }

    private bool IsLineBlockedByWorldEffect(PointF start, PointF end)
    {
        return _worldEffects.Any(effect =>
            effect.Kind is WorldEffectKind.NanoSmoke or WorldEffectKind.PoisonCloud &&
            SegmentIntersectsCircle(start, end, effect.Position, effect.Radius));
    }

    private static bool SegmentIntersectsCircle(PointF start, PointF end, PointF center, float radius)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= 0.001f)
        {
            return Distance(start, center) <= radius;
        }

        var t = (((center.X - start.X) * dx) + ((center.Y - start.Y) * dy)) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);
        var closest = new PointF(start.X + (dx * t), start.Y + (dy * t));
        return Distance(closest, center) <= radius;
    }
}
