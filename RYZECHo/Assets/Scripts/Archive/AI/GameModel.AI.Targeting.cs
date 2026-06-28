namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Targeting — ターゲット選択ロジック
    // =========================================================================

    private Actor? PickBestTarget(PointF origin, float range, ActorType sourceType)
    {
        IEnumerable<Actor> candidates = sourceType == ActorType.Enemy
            ? LivePlayerTeam()
            : _enemies.Where(actor => actor.IsAlive);

        var target = candidates
            .Where(actor => Distance(origin, actor.Position) <= range && HasLineOfSight(sourceType, origin, actor.Position))
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
            .Where(actor => actor.Type != ActorType.Player || _playerGhostTimer <= 0f)
            .Where(actor => actor.GhostTimer <= 0f)
            .Where(actor => Distance(enemy.Position, actor.Position) <= _weaponStats[enemy.Weapon].ProjectileRange + 30f)
            .OrderBy(actor => Distance(enemy.Position, actor.Position))
            .ToList();

        return defenders.FirstOrDefault(actor => HasLineOfSight(enemy, actor.Position));
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
            if (Distance(closest, enemy.Position) <= enemy.Radius + 5f && projection < bestDistance && HasLineOfSight(_player, enemy.Position))
            {
                best = enemy;
                bestDistance = projection;
            }
        }

        return best;
    }

    private void RevealEnemiesInActorVision(Actor actor, float duration = SharedVisionDurationSeconds)
    {
        foreach (var enemy in _enemies.Where(enemy => enemy.IsAlive && ActorHasDirectSightTo(actor, enemy.Position)))
        {
            RevealEnemyToTeam(enemy, duration);
        }
    }
}
