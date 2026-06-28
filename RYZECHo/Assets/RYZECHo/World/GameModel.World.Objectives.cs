namespace RYZECHo;

public sealed partial class GameModel
{
    private ObjectiveSite[] GetBombSites()
    {
        var defenseSites = new[]
        {
            new ObjectiveSite(ObjectiveSiteId.Alpha, "A", new Point(14, 4)),
            new ObjectiveSite(ObjectiveSiteId.Bravo, "B", new Point(14, 8)),
        };

        if (!IsPlayerTeamAttacking())
        {
            return defenseSites;
        }

        return defenseSites
            .Select(site => site with { Cell = MirrorCellHorizontally(site.Cell) })
            .ToArray();
    }

    private ObjectiveSite GetBombSite(ObjectiveSiteId siteId)
    {
        return GetBombSites().First(site => site.Id == siteId);
    }

    private ObjectiveSiteId CurrentObjectiveSiteId()
    {
        if (_armedBombSiteId is not null)
        {
            return _armedBombSiteId.Value;
        }

        if (_activePlanter is not null && TryGetBombSiteAt(_activePlanter.Position, out var activeSite, 10f))
        {
            return activeSite.Id;
        }

        return _attackFocusSite;
    }

    private string CurrentObjectiveSiteLabel()
    {
        return GetBombSite(CurrentObjectiveSiteId()).Label;
    }

    private bool TryGetBombSiteAt(PointF position, out ObjectiveSite site, float padding = 0f)
    {
        foreach (var candidate in GetBombSites())
        {
            if (Distance(position, CellCenter(candidate.Cell)) <= BombSiteRadius + padding)
            {
                site = candidate;
                return true;
            }
        }

        site = default;
        return false;
    }

    private ObjectiveSite FindClosestSite(PointF position)
    {
        return GetBombSites()
            .OrderBy(site => Distance(position, CellCenter(site.Cell)))
            .First();
    }

    private ObjectiveSiteId ChooseAttackFocusSite()
    {
        var parity = (_currentRound + _playerRoundWins + _enemyRoundWins) % 2;
        return parity == 0 ? ObjectiveSiteId.Alpha : ObjectiveSiteId.Bravo;
    }
}
