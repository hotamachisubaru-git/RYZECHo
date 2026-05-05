namespace RYZECHo.Prototype;

internal readonly record struct ObjectiveSite(ObjectiveSiteId Id, string Label, Point Cell);

internal readonly record struct ActorBlueprint(
    string Name,
    ActorType Type,
    Point HomeCell,
    WeaponType Weapon,
    float Radius,
    float MaxHealth,
    float MaxShield,
    float HearingRange,
    float BaseMoveSpeed);

internal static class RosterCatalog
{
    public const string PlayerName = "あなた";
    public const string NorthAnchorName = "北アンカー";
    public const string SouthAnchorName = "南アンカー";
    public const string CenterLinkName = "中央リンク";

    public static readonly ActorBlueprint Player = new(
        PlayerName,
        ActorType.Player,
        new Point(13, 6),
        WeaponType.Giant,
        14f,
        100f,
        60f,
        350f,
        210f);

    public static readonly ActorBlueprint[] Allies =
    [
        new(
            NorthAnchorName,
            ActorType.Ally,
            new Point(13, 4),
            WeaponType.Violet,
            13f,
            95f,
            42f,
            300f,
            168f),
        new(
            SouthAnchorName,
            ActorType.Ally,
            new Point(13, 8),
            WeaponType.Blitz,
            13f,
            95f,
            36f,
            420f,
            188f),
        new(
            CenterLinkName,
            ActorType.Ally,
            new Point(12, 6),
            WeaponType.Fairy,
            13f,
            95f,
            48f,
            340f,
            176f),
    ];

    public static WeaponType DefaultFriendlyWeaponFor(string actorName)
    {
        return actorName switch
        {
            PlayerName => Player.Weapon,
            NorthAnchorName => WeaponType.Violet,
            SouthAnchorName => WeaponType.Blitz,
            CenterLinkName => WeaponType.Fairy,
            _ => Player.Weapon,
        };
    }
}

internal sealed class WeaponStats
{
    public required WeaponType Type { get; init; }
    public required string Label { get; init; }
    public required string ShortLabel { get; init; }
    public required string Code { get; init; }
    public required string Category { get; init; }
    public required string VisionClass { get; init; }
    public required int Cost { get; init; }
    public required int MagazineAmmo { get; init; }
    public required int ReserveAmmo { get; init; }
    public required float VisionRange { get; init; }
    public required float HearingMultiplier { get; init; }
    public required float FireCooldown { get; init; }
    public required float Damage { get; init; }
    public required float MoveSpeed { get; init; }
    public required float ProjectileRange { get; init; }
    public required bool ScopedFov { get; init; }
}

internal sealed class Structure
{
    public required StructureKind Kind { get; init; }
    public required Point Cell { get; init; }
    public required int APCost { get; init; }
    public required string Label { get; init; }
    public float Health { get; set; }
    public float MaxHealth { get; init; }
    public float PulseCooldown { get; set; }
}

internal sealed class Ripple
{
    public required PointF Position { get; init; }
    public required float Strength { get; init; }
    public required float Lifetime { get; init; }
    public required RippleKind Kind { get; init; }
    public required Color Color { get; init; }
    public float Age { get; set; }
}

internal sealed class Actor
{
    public required string Name { get; init; }
    public required ActorType Type { get; init; }
    public required Point HomeCell { get; init; }
    public required WeaponType Weapon { get; set; }
    public required PointF Position { get; set; }
    public required float Radius { get; init; }
    public required float MaxHealth { get; init; }
    public required float MaxShield { get; init; }
    public required float HearingRange { get; init; }
    public required float BaseMoveSpeed { get; init; }
    public float Health { get; set; }
    public float Shield { get; set; }
    public float ShieldRegenDelay { get; set; }
    public float FireCooldown { get; set; }
    public float PathCooldown { get; set; }
    public float FootstepCooldown { get; set; }
    public int FootstepPulseIndex { get; set; }
    public float FacingAngle { get; set; }
    public bool IsBoss { get; set; }
    public Queue<PointF> Path { get; } = new();
    public bool IsAlive => Health > 0.01f;
}
