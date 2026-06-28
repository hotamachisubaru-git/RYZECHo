namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Movement — AI移動・パス追従
    // =========================================================================

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
        var amplifiedSurface = _structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell);
        if (amplifiedSurface)
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

        var cadence = AudioRippleVisualRules.AdvanceFootstepCadence(actor.FootstepPulseIndex, speed, amplifiedSurface);
        actor.FootstepPulseIndex = cadence.NextPulseIndex;
        if (cadence.EmitsRipple)
        {
            EmitRipple(actor.Position, cadence.RippleStrength, RippleKind.Footstep, Color.FromArgb(250, 248, 248, 248));
        }

        actor.FootstepCooldown = cadence.CooldownSeconds;
    }
}
