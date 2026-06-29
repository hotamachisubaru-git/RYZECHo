namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Pathfinding — パス検索・ナビゲーション
    // =========================================================================

    private List<Point> FindPath(Point start, Point goal)
    {
        if (start == goal)
        {
            return new List<Point> { start };
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
            return new List<Point>();
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

        return _structures.Any(structure => IsRouteBlockingStructure(structure.Kind) && structure.Cell == cell && structure.Health > 0f);
    }
}
