namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Goal Picking — AIの目標地点選択
    // =========================================================================

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
}
