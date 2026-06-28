namespace RYZECHo;

public sealed partial class GameModel
{
    private IEnumerable<Actor> LivePlayerTeam()
    {
        if (_player.IsAlive)
        {
            yield return _player;
        }

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            yield return ally;
        }
    }

    private int LiveEnemyCount()
    {
        return _enemies.Count(enemy => enemy.IsAlive);
    }

    private bool IsPlayerTeamAttacking()
    {
        return _playerTeamRole == TeamRole.Attack;
    }

    private bool EnemyTeamAttacking()
    {
        return !IsPlayerTeamAttacking();
    }

    private static TeamRole ToggleRole(TeamRole role)
    {
        return role == TeamRole.Attack ? TeamRole.Defense : TeamRole.Attack;
    }

    private bool HasMatchWinner()
    {
        return IsWinningScore(_playerRoundWins, _enemyRoundWins) || IsWinningScore(_enemyRoundWins, _playerRoundWins);
    }

    private static bool IsWinningScore(int score, int opponentScore)
    {
        if (score < RoundsToWin)
        {
            return false;
        }

        if (score >= OvertimeTriggerScore && opponentScore >= OvertimeTriggerScore)
        {
            return score - opponentScore >= 2;
        }

        return true;
    }

    private int CurrentAttackerCount()
    {
        return IsPlayerTeamAttacking() ? LivePlayerTeam().Count() : LiveEnemyCount();
    }

    private int CurrentDefenderCount()
    {
        return IsPlayerTeamAttacking() ? LiveEnemyCount() : LivePlayerTeam().Count();
    }

    private string PlayerRoleLabel()
    {
        return IsPlayerTeamAttacking() ? "攻撃側" : "防衛側";
    }

    private string PlayerRoleShortLabel()
    {
        return IsPlayerTeamAttacking() ? "攻撃" : "防衛";
    }

    private int AttackApproachDirection()
    {
        return GetBombSite(_bombPlanted && _armedBombSiteId is not null ? _armedBombSiteId.Value : _attackFocusSite).Cell.X < GridColumns / 2 ? 1 : -1;
    }

    private Point[] GetEnemySetupCells()
    {
        if (EnemyTeamAttacking())
        {
            return _spawnCells.ToArray();
        }

        return new[]
        {
            MirrorCellHorizontally(_player.HomeCell),
            MirrorCellHorizontally(_allies[0].HomeCell),
            MirrorCellHorizontally(_allies[1].HomeCell),
            MirrorCellHorizontally(_allies[2].HomeCell),
        };
    }

}
