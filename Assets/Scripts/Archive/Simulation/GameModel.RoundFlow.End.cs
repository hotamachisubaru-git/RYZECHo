namespace RYZECHo;

internal sealed partial class GameModel
{
    private void EndRound(bool won, string? outcomeSummary = null)
    {
        var bossAlive = SelectedBoss()?.IsAlive ?? false;
        var bossPayout = BossEconomyRules.CalculateRoundPayout(
            _bossInvestments,
            won,
            bossAlive,
            _roundBossKillCount,
            BossPayoutMultiplier);
        var completedRound = _currentRound;
        var integrityLocked = IsIntegrityRewardsLocked();

        if (won)
        {
            _playerRoundWins++;
        }
        else
        {
            _enemyRoundWins++;
        }

        var economySummary = integrityLocked
            ? "整合性違反検知によりラウンド報酬凍結"
            : won
                ? $"勝利報酬 +{WinRewardCredits}c"
                : $"敗北補償 +{LossRewardCredits}c";

        if (!integrityLocked)
        {
            _credits += won ? WinRewardCredits : LossRewardCredits;

            if (bossPayout.TotalInvestedCredits > 0)
            {
                if (bossPayout.InvestmentReturned)
                {
                    _credits += bossPayout.TotalReturnedCredits;
                    economySummary += $" / 投資返還 +{bossPayout.TotalReturnedCredits}c";
                    PushActivityFeed($"投資返還配分: {FormatBossReturnAllocation(bossPayout)}");
                }
                else
                {
                    economySummary += $" / {bossPayout.Reason}";
                }
            }
        }

        var resultSummary = $"{outcomeSummary ?? (won ? "ラウンド勝利。" : "ラウンド敗北。")} {economySummary}。";

        if (HasMatchWinner())
        {
            _resultDestination = won ? GamePhase.Victory : GamePhase.Defeat;
            AwardMatchProgression(won);
            SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。{_lastProgressionSummary}");
        }
        else
        {
            _currentRound++;

            var enteredOvertime = !_isOvertime && _playerRoundWins == OvertimeTriggerScore && _enemyRoundWins == OvertimeTriggerScore;
            if (enteredOvertime)
            {
                _isOvertime = true;
            }

            if (completedRound == RegulationSideSwitchRound)
            {
                _playerTeamRole = ToggleRole(_playerTeamRole);
                _resultDestination = GamePhase.Construct;
                _sideSwapConstructPending = true;
                _buildPoints = MapEditApRules.RefillForEditPhase(_buildPoints, SideSwapBuildPointRefill, MaxBuildPoints).AfterAp;
                SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。第4ラウンド終了につき攻守交代、再エディットを開始します。");
            }
            else
            {
                _resultDestination = GamePhase.Bet;
                if (enteredOvertime)
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。6-6 のためオーバータイムに突入します。");
                }
                else
                {
                    SetResultMessage($"{resultSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}。");
                }
            }
        }

        ResetAgentRuntimeState(clearWorldEffects: true);
        _phase = GamePhase.RoundResult;
        _resultTimer = 2.4f;
    }

    private static string FormatBossReturnAllocation(BossRoundPayout payout)
    {
        var allocations = payout.Returns
            .Where(entry => entry.ReturnedCredits > 0)
            .Select(entry => $"{entry.InvestorName}+{entry.ReturnedCredits}c")
            .ToArray();

        return allocations.Length == 0 ? payout.Reason : string.Join(" / ", allocations);
    }
}
