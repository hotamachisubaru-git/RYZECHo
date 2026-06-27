using System;

namespace RYZECHo;

/// <summary>
/// 構造物破壊イベント。
/// </summary>
internal sealed class StructureDestroyedEvent : GameEvent
{
    public StructureKind Type { get; }
    public Point Cell { get; }
    public PointF Position { get; }

    public StructureDestroyedEvent(StructureKind type, Point cell, PointF position)
    {
        Type = type;
        Cell = cell;
        Position = position;
    }
}
