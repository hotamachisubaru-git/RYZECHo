using System;

namespace RYZECHo;

/// <summary>
/// オーディオキュー発生イベント。
/// </summary>
internal sealed class AudioCueEvent : GameEvent
{
    public RippleKind Kind { get; }
    public PointF Position { get; }
    public float Strength { get; }

    public AudioCueEvent(RippleKind kind, PointF position, float strength)
    {
        Kind = kind;
        Position = position;
        Strength = strength;
    }
}
