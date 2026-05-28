namespace RYZECHo.Prototype;

internal readonly record struct IntegrityActorSnapshot(
    PointF Position,
    float Health,
    float Shield,
    bool WasAlive,
    WeaponType Weapon);
