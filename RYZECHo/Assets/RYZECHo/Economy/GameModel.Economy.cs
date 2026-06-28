namespace RYZECHo;

public sealed partial class GameModel
{
    private Actor? FriendlyActorByName(string actorName)
    {
        if (actorName == _player.Name)
        {
            return _player;
        }

        return _allies.FirstOrDefault(actor => actor.Name == actorName);
    }

    private void EnsureFriendlyEconomyState()
    {
        foreach (var actorName in BossCandidateNames())
        {
            if (!_bossInvestments.ContainsKey(actorName))
            {
                _bossInvestments[actorName] = actorName == _player.Name ? OptimalBossInvestment : 0;
            }

            if (!_ultPoints.ContainsKey(actorName))
            {
                _ultPoints[actorName] = 0;
            }
        }
    }

    private int GetFriendlyInvestment(string actorName)
    {
        return _bossInvestments.TryGetValue(actorName, out var amount) ? amount : 0;
    }

    private void SetFriendlyInvestment(string actorName, int amount)
    {
        EnsureFriendlyEconomyState();
        _bossInvestments[actorName] = Math.Max(0, amount);
        SyncSelectedBetTotal();
    }

    private void AdjustSelectedInvestment(int delta)
    {
        EnsureFriendlyEconomyState();
        var actorName = _selectedBossName;
        var otherInvestments = TotalSelectedInvestment() - GetFriendlyInvestment(actorName);
        var maxInvestment = Math.Max(0, _credits - _weaponStats[_selectedWeapon].Cost - _weaponStats[_selectedSidearmWeapon].Cost - otherInvestments);
        var next = Math.Clamp(GetFriendlyInvestment(actorName) + delta, 0, maxInvestment);
        _bossInvestments[actorName] = next;
        SyncSelectedBetTotal();
    }

    private int TotalSelectedInvestment()
    {
        return BossCandidateNames().Sum(GetFriendlyInvestment);
    }

    private int SelectedBossInvestment()
    {
        return GetFriendlyInvestment(_selectedBossName);
    }

    private void SyncSelectedBetTotal()
    {
        _selectedBet = TotalSelectedInvestment();
    }

    private int GetUltPoints(string actorName)
    {
        EnsureFriendlyEconomyState();
        return _ultPoints.TryGetValue(actorName, out var amount) ? amount : 0;
    }

    private void AwardUltPoints(string actorName, int amount, string reason)
    {
        if (!_ultPoints.ContainsKey(actorName))
        {
            return;
        }

        var award = BossEconomyRules.CalculateUltAward(actorName, _ultPoints[actorName], amount, MaxUltPoints, reason);
        _ultPoints[actorName] = award.After;
        if (award.Granted > 0)
        {
            PushActivityFeed($"{actorName} ULT +{award.Granted} ({reason})。{award.After}/{MaxUltPoints}");
        }
    }

    private int SelectedBossUltPoints()
    {
        return GetUltPoints(_selectedBossName);
    }
}
