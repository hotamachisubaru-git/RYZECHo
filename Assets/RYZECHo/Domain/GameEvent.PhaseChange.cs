using System;

namespace RYZECHo;

/// <summary>
/// ラウンドフェーズ遷移イベント。
/// </summary>
internal sealed class PhaseChangeEvent : GameEvent
{
    public GamePhase From { get; }
    public GamePhase To { get; }

    public PhaseChangeEvent(GamePhase from, GamePhase to)
    {
        From = from;
        To = to;
    }
}
