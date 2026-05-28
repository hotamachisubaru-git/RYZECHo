namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    public void Update(float deltaSeconds, InputSnapshot input)
    {
        PrepareIntegrityFrame(deltaSeconds);
        _uiPulseTime += deltaSeconds;
        UpdateMonetizationRuntime(deltaSeconds);
        UpdateRipples(deltaSeconds);

        switch (_phase)
        {
            case GamePhase.Construct:
                UpdateConstructPhase(input);
                break;
            case GamePhase.Bet:
                UpdateBetPhase(input);
                break;
            case GamePhase.Hunt:
                UpdateHuntPhase(deltaSeconds, input);
                break;
            case GamePhase.RoundResult:
                UpdateRoundResult(deltaSeconds, input);
                break;
            case GamePhase.Victory:
            case GamePhase.Defeat:
                UpdateEndState(input);
                break;
        }

        FinalizeIntegrityFrame(deltaSeconds);
    }
}
