namespace RYZECHo;

/// <summary>
/// ゲームイベントの基底クラス。
/// </summary>
internal abstract class GameEvent
{
    public double Timestamp { get; protected set; }
}
