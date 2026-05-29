namespace RYZECHo;

internal sealed partial class GameModel
{
    private string? ValidateStructurePlacement(Structure candidate)
    {
        var placement = MapEditApRules.ValidatePlacement(candidate, _structures, _buildSlots, _noBuildZones);
        if (!placement.Allowed)
        {
            return placement.Reason;
        }

        if (IsRouteBlockingStructure(candidate.Kind) && !PreservesAttackRoutes(candidate.Cell))
        {
            return "禁止：全ルートの封鎖。最低 1 つのサイト到達経路が必要です。";
        }

        if (IsRouteBlockingStructure(candidate.Kind) && !PreservesThreeLaneAccess(candidate.Cell))
        {
            return "レギュレーション違反：3 レーン（上下中）のいずれかが完全に塞がれています。";
        }

        return null;
    }

    private bool IsNoBuildCell(Point cell)
    {
        return MapEditApRules.IsNoBuildCell(cell, _noBuildZones);
    }

    private HashSet<Point> BuildBlockedCells(Point? candidateDoorCell = null)
    {
        var blocked = _permanentWalls.ToHashSet();
        foreach (var door in _structures.Where(structure => IsRouteBlockingStructure(structure.Kind) && structure.Health > 0f))
        {
            blocked.Add(door.Cell);
        }

        if (candidateDoorCell is not null)
        {
            blocked.Add(candidateDoorCell.Value);
        }

        return blocked;
    }

    private bool PreservesAttackRoutes(Point? candidateDoorCell = null)
    {
        var blocked = BuildBlockedCells(candidateDoorCell);
        return GetBombSites().All(site => HasPathToTarget(CurrentAttackerEntryCells(), site.Cell, blocked));
    }

    private bool HasPathToTarget(IEnumerable<Point> entries, Point target, HashSet<Point> blocked)
    {
        if (blocked.Contains(target))
        {
            return false;
        }

        var frontier = new Queue<Point>();
        var visited = new HashSet<Point>();
        foreach (var entry in entries)
        {
            if (blocked.Contains(entry) || !visited.Add(entry))
            {
                continue;
            }

            frontier.Enqueue(entry);
        }

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == target)
            {
                return true;
            }

            foreach (var neighbor in Neighbors(current))
            {
                if (blocked.Contains(neighbor) || !visited.Add(neighbor))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
            }
        }

        return false;
    }

    private static bool IsRouteBlockingStructure(StructureKind kind)
    {
        return kind is StructureKind.BlastDoor or StructureKind.PortableCover or StructureKind.VisorWall;
    }

    private bool PreservesThreeLaneAccess(Point? candidateDoorCell = null)
    {
        var blocked = BuildBlockedCells(candidateDoorCell);
        return HasLaneTraverse(blocked, 1, 3) &&
               HasLaneTraverse(blocked, 4, 7) &&
               HasLaneTraverse(blocked, 8, 10);
    }

    private bool HasLaneTraverse(HashSet<Point> blocked, int minY, int maxY)
    {
        var entries = CurrentAttackerEntryCells()
            .Where(cell => cell.Y >= minY && cell.Y <= maxY)
            .ToArray();

        if (entries.Length == 0)
        {
            var startX = IsPlayerTeamAttacking() ? GridColumns - 2 : 1;
            entries = Enumerable.Range(minY, (maxY - minY) + 1)
                .Select(y => new Point(startX, y))
                .Where(cell => !blocked.Contains(cell))
                .ToArray();
        }

        var frontier = new Queue<Point>();
        var visited = new HashSet<Point>();
        foreach (var entry in entries)
        {
            if (blocked.Contains(entry) || !visited.Add(entry))
            {
                continue;
            }

            frontier.Enqueue(entry);
        }

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (IsLaneTraverseGoal(current))
            {
                return true;
            }

            foreach (var neighbor in Neighbors(current))
            {
                if (neighbor.Y < minY || neighbor.Y > maxY || blocked.Contains(neighbor) || !visited.Add(neighbor))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
            }
        }

        return false;
    }

    private bool IsLaneTraverseGoal(Point cell)
    {
        return IsPlayerTeamAttacking()
            ? cell.X <= (GridColumns / 2)
            : cell.X >= (GridColumns / 2) - 1;
    }

    private IEnumerable<Point> CurrentAttackerEntryCells()
    {
        if (IsPlayerTeamAttacking())
        {
            yield return _player.HomeCell;

            foreach (var ally in _allies)
            {
                yield return ally.HomeCell;
            }

            yield break;
        }

        foreach (var spawnCell in _spawnCells)
        {
            yield return spawnCell;
        }
    }

}
