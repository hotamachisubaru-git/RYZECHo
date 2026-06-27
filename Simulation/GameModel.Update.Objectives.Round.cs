namespace RYZECHo;

internal sealed partial class GameModel
{
    private void UpdateHuntPhase(float deltaSeconds, InputSnapshot input)
    {
        _roundTimer -= deltaSeconds;
        _pingCooldown -= deltaSeconds;
        UpdateSharedVision(deltaSeconds);
        UpdateAgentRuntime(deltaSeconds, input);

        RestoreBossFlags();
        UpdatePlayer(deltaSeconds, input);
        UpdateAllies(deltaSeconds);
        UpdateEnemies(deltaSeconds);
        UpdateStructures(deltaSeconds);

        if (IsPlayerTeamAttacking())
        {
            ResolveAttackingRoundState(deltaSeconds, input);
            return;
        }

        ResolveDefendingRoundState(deltaSeconds, input);
    }

    private void ResolveAttackingRoundState(float deltaSeconds, InputSnapshot input)
    {
        if (LiveEnemyCount() == 0)
        {
            EndRound(true, _bombPlanted ? "守備班壊滅。爆破を待てば突破成立です。" : "守備班壊滅。設置前にサイトを制圧しました。");
            return;
        }

        if (!LivePlayerTeam().Any() && !_bombPlanted)
        {
            EndRound(false, "攻撃班壊滅。設置に失敗しました。");
            return;
        }

        if (UpdateBombObjective(deltaSeconds, input))
        {
            return;
        }

        if (_coreHealth <= 0f)
        {
            EndRound(true, "ボム爆破成功。サイトを突破しました。");
            return;
        }

        if (!_bombPlanted && _roundTimer <= 0f)
        {
            EndRound(false, "設置猶予が終了。攻撃失敗です。");
        }
    }

    private void ResolveDefendingRoundState(float deltaSeconds, InputSnapshot input)
    {
        if (!_bombPlanted && LiveEnemyCount() == 0)
        {
            EndRound(true, "襲撃班を排除。設置前に制圧しました。");
            return;
        }

        if (!LivePlayerTeam().Any())
        {
            EndRound(false, _bombPlanted ? "防衛班壊滅。解除できずサイトを失いました。" : "防衛班壊滅。サイト防衛に失敗しました。");
            return;
        }

        if (UpdateBombObjective(deltaSeconds, input))
        {
            return;
        }

        if (_coreHealth <= 0f)
        {
            EndRound(false, "ボムが爆発。サイト防衛に失敗しました。");
            return;
        }

        if (!_bombPlanted && _roundTimer <= 0f)
        {
            EndRound(true, "設置猶予を守り切りました。");
        }
    }

    private void UpdateRoundResult(float deltaSeconds, InputSnapshot input)
    {
        _resultTimer -= deltaSeconds;

        if (input.Confirm || _resultTimer <= 0f)
        {
            if (_resultDestination == GamePhase.Bet)
            {
                BeginBetPhase();
            }
            else if (_resultDestination == GamePhase.Construct)
            {
                EnterSideSwapConstructPhase();
            }
            else
            {
                _phase = _resultDestination;
            }
        }
    }

    private void UpdateEndState(InputSnapshot input)
    {
        if (input.PressR)
        {
            ResetCampaign();
        }
    }
}
