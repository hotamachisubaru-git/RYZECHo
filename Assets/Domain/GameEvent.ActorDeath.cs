using System;

namespace RYZECHo;

/// <summary>
/// エンティティ死亡イベント。
/// </summary>
internal sealed class ActorDeathEvent : GameEvent
{
    public string VictimName { get; }
    public ActorType Kind { get; }
    public PointF Position { get; }
    public string? KillerName { get; }

    public ActorDeathEvent(string victimName, ActorType kind, PointF position, string? killerName = null)
    {
        VictimName = victimName;
        Kind = kind;
        Position = position;
        KillerName = killerName;
    }
}
