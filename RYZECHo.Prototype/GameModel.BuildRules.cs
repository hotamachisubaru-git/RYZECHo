namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private string? ValidateStructurePlacement(Structure candidate)
    {
        if (IsNoBuildCell(candidate.Cell))
        {
            return "そのセルはノー・ビルド・ゾーンです。";
        }

        if (candidate.Kind is StructureKind.HoneyTrap or StructureKind.StaticNest && ViolatesTrapDensity(candidate))
        {
            return "同カテゴリのトラップが近すぎます。デッドゾーンを空けてください。";
        }

        if (candidate.Kind == StructureKind.BlastDoor && WouldExceedBlastDoorClusterLimit(candidate.Cell))
        {
            return "強化扉の連結上限を超えます。";
        }

        if (candidate.Kind == StructureKind.BlastDoor && !PreservesAttackRoute(candidate.Cell))
        {
            return "主要ルートを全封鎖する配置はできません。";
        }

        return null;
    }

    private bool IsNoBuildCell(Point cell)
    {
        return _noBuildZones.Contains(cell);
    }

    private bool ViolatesTrapDensity(Structure candidate)
    {
        return _structures.Any(structure =>
            structure.Kind is StructureKind.HoneyTrap or StructureKind.StaticNest &&
            Distance(CellCenter(structure.Cell), CellCenter(candidate.Cell)) < CellSize * 2.2f);
    }

    private bool WouldExceedBlastDoorClusterLimit(Point newDoorCell)
    {
        var doorCells = _structures
            .Where(structure => structure.Kind == StructureKind.BlastDoor && structure.Health > 0f)
            .Select(structure => structure.Cell)
            .ToHashSet();
        doorCells.Add(newDoorCell);

        var cluster = new Queue<Point>();
        cluster.Enqueue(newDoorCell);
        var visited = new HashSet<Point> { newDoorCell };

        while (cluster.Count > 0)
        {
            var current = cluster.Dequeue();
            foreach (var neighbor in Neighbors(current))
            {
                if (!doorCells.Contains(neighbor) || !visited.Add(neighbor))
                {
                    continue;
                }

                cluster.Enqueue(neighbor);
            }
        }

        return visited.Count > 2;
    }

    private bool PreservesAttackRoute(Point? candidateDoorCell = null)
    {
        var blocked = _permanentWalls.ToHashSet();
        foreach (var door in _structures.Where(structure => structure.Kind == StructureKind.BlastDoor && structure.Health > 0f))
        {
            blocked.Add(door.Cell);
        }

        if (candidateDoorCell is not null)
        {
            blocked.Add(candidateDoorCell.Value);
        }

        var target = GetBombSiteCell();
        if (blocked.Contains(target))
        {
            return false;
        }

        var frontier = new Queue<Point>();
        var visited = new HashSet<Point>();
        foreach (var entry in CurrentAttackerEntryCells())
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

    private void UpdatePlayerIdleState(float deltaSeconds, bool acted)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            _playerIdleSeconds = 0f;
            return;
        }

        var wasExposed = IsPlayerBreathingExposed();
        if (acted)
        {
            _playerIdleSeconds = 0f;
            return;
        }

        _playerIdleSeconds += deltaSeconds;
        if (!wasExposed && IsPlayerBreathingExposed())
        {
            PushActivityFeed("10 秒以上静止したため呼吸音が増幅。近距離の敵に位置が漏れやすくなります。");
        }
    }

    private bool IsPlayerBreathingExposed()
    {
        return _phase == GamePhase.Hunt && _player.IsAlive && _playerIdleSeconds >= 10f;
    }
}
