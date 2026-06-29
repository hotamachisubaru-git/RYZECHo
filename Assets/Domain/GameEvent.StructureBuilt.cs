using System;

namespace RYZECHo;

/// <summary>
/// 構造物設置イベント。
/// </summary>
internal sealed class StructureBuiltEvent : GameEvent
{
    public StructureKind Type { get; }
    public Point Cell { get; }
    public PointF Position { get; }
    public string BuilderName { get; }

    public StructureBuiltEvent(StructureKind type, Point cell, PointF position, string builderName)
    {
        Type = type;
        Cell = cell;
        Position = position;
        BuilderName = builderName;
    }
}
