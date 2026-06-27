namespace RYZECHo;

/// <summary>
/// フェーズ遷移イベント（GamePhase 単一パラメータ版）。
/// 遷移先フェーズのみを通知する軽量イベント。
/// </summary>
internal sealed class GamePhaseChangedEvent : GameEvent
{
    public GamePhase To { get; }

    public GamePhaseChangedEvent(GamePhase to)
    {
        To = to;
    }
}
