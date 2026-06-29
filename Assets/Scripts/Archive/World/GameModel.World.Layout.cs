namespace RYZECHo;

internal sealed partial class GameModel
{
    private bool CanPlayerDefuse()
    {
        return _bombPlanted &&
               _player.IsAlive &&
               _armedBombSiteId is not null &&
               IsInsideBombSite(_player.Position, _armedBombSiteId.Value, 10f);
    }

    private bool IsInsideBombSite(PointF position, float padding = 0f)
    {
        return GetBombSites().Any(site => IsInsideBombSite(position, site.Id, padding));
    }

    private bool IsInsideBombSite(PointF position, ObjectiveSiteId siteId, float padding = 0f)
    {
        return Distance(position, BombSitePosition(siteId)) <= BombSiteRadius + padding;
    }

    private void BuildMapGeometry()
    {
        _permanentWalls.Clear();
        _buildSlots.Clear();
        _noBuildZones.Clear();
        _spawnCells.Clear();

        for (var x = 0; x < GridColumns; x++)
        {
            _permanentWalls.Add(new Point(x, 0));
            _permanentWalls.Add(new Point(x, GridRows - 1));
        }

        for (var y = 0; y < GridRows; y++)
        {
            _permanentWalls.Add(new Point(0, y));
            _permanentWalls.Add(new Point(GridColumns - 1, y));
        }

        AddWallLine(6, 2, 6, 4);
        AddWallLine(6, 7, 6, 9);
        AddWallLine(9, 2, 9, 4);
        AddWallLine(9, 7, 9, 9);
        AddWallLine(12, 2, 12, 3);
        AddWallLine(12, 8, 12, 9);
        AddWallLine(3, 5, 4, 5);
        AddWallLine(10, 5, 11, 5);

        foreach (var slot in new[]
        {
            new Point(3, 4), new Point(3, 6), new Point(5, 5), new Point(6, 5), new Point(6, 6),
            new Point(7, 4), new Point(7, 7), new Point(8, 5), new Point(8, 6), new Point(9, 5),
            new Point(10, 6), new Point(11, 4), new Point(11, 7), new Point(13, 5), new Point(13, 7),
        })
        {
            if (!_permanentWalls.Contains(slot))
            {
                _buildSlots.Add(slot);
            }
        }

        _spawnCells.AddRange(new[] { new Point(1, 2), new Point(1, 4), new Point(1, 7), new Point(1, 9) });

        foreach (var protectedCell in new[]
        {
            new Point(14, 4), new Point(13, 4), new Point(14, 5), new Point(14, 3),
            new Point(14, 8), new Point(13, 8), new Point(14, 7), new Point(14, 9),
            MirrorCellHorizontally(new Point(14, 4)), MirrorCellHorizontally(new Point(13, 4)), MirrorCellHorizontally(new Point(14, 5)), MirrorCellHorizontally(new Point(14, 3)),
            MirrorCellHorizontally(new Point(14, 8)), MirrorCellHorizontally(new Point(13, 8)), MirrorCellHorizontally(new Point(14, 7)), MirrorCellHorizontally(new Point(14, 9)),
            new Point(1, 2), new Point(2, 2), new Point(1, 4), new Point(2, 4), new Point(1, 7), new Point(2, 7), new Point(1, 9), new Point(2, 9),
            new Point(13, 6), new Point(13, 4), new Point(13, 8), new Point(12, 6),
            new Point(12, 6), new Point(12, 4), new Point(12, 8), new Point(11, 6),
        })
        {
            if (protectedCell.X >= 0 && protectedCell.X < GridColumns && protectedCell.Y >= 0 && protectedCell.Y < GridRows)
            {
                _noBuildZones.Add(protectedCell);
            }
        }
    }

    private void AddWallLine(int startX, int startY, int endX, int endY)
    {
        for (var x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
        {
            for (var y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
            {
                _permanentWalls.Add(new Point(x, y));
            }
        }
    }

    private static bool IsPerimeterCell(Point cell)
    {
        return cell.X == 0 || cell.Y == 0 || cell.X == GridColumns - 1 || cell.Y == GridRows - 1;
    }

}
