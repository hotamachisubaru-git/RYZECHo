namespace RYZECHo.Prototype;

internal readonly record struct ObjectiveSite(ObjectiveSiteId Id, string Label, Point Cell);

internal readonly record struct ActorBlueprint(
    string Name,
    AgentKind Agent,
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
        AgentKind.Veil,
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
            AgentKind.Vine,
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
            AgentKind.Nitro,
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
            AgentKind.Oasis,
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

internal sealed class StructureStats
{
    public required string Label { get; init; }
    public required int ApCost { get; init; }
    public required float MaxHealth { get; init; }
    public bool BlocksMovement { get; init; }
    public bool AiTargetable { get; init; }
}

internal static class StructureCatalog
{
    private static readonly Dictionary<StructureKind, StructureStats> _catalog = new()
    {
        { StructureKind.BlastDoor, new() { Label = "強化ナノ・ゲート", ApCost = 5, MaxHealth = 450f, BlocksMovement = true, AiTargetable = true } },
        { StructureKind.HoneyTrap, new() { Label = "ハチミツ・パッチ", ApCost = 2, MaxHealth = 60f, BlocksMovement = false, AiTargetable = false } },
        { StructureKind.StaticNest, new() { Label = "スタティック・ネスト", ApCost = 3, MaxHealth = 100f, BlocksMovement = false, AiTargetable = true } },
        { StructureKind.ReconBeacon, new() { Label = "リコンビーコン", ApCost = 3, MaxHealth = 40f, BlocksMovement = false, AiTargetable = true } },
        { StructureKind.ShieldRelay, new() { Label = "シールドリレー", ApCost = 4, MaxHealth = 180f, BlocksMovement = false, AiTargetable = true } },
        { StructureKind.PortableCover, new() { Label = "ポータブルカバー", ApCost = 3, MaxHealth = 300f, BlocksMovement = true, AiTargetable = true } },
        { StructureKind.VisorWall, new() { Label = "バイザー壁", ApCost = 4, MaxHealth = 200f, BlocksMovement = true, AiTargetable = true } },
        { StructureKind.HoloDecoy, new() { Label = "ホログラムデコイ", ApCost = 2, MaxHealth = 1f, BlocksMovement = false, AiTargetable = true } },
    };

    public static StructureStats Get(StructureKind kind) => _catalog[kind];

    public static string Label(BuildToolKind tool) => tool switch {
        BuildToolKind.BlastDoor => _catalog[StructureKind.BlastDoor].Label,
        BuildToolKind.HoneyTrap => _catalog[StructureKind.HoneyTrap].Label,
        // ... (他のマッピングも同様に定義)
        _ => "未知の設備"
    };
}

internal sealed class WorldEffectStats
{
    public required float Radius { get; init; }
    public required float Lifetime { get; init; }
    public required Color Color { get; init; }
    public bool BlocksVision { get; init; }
}

internal static class WorldEffectCatalog
{
    private static readonly Dictionary<WorldEffectKind, WorldEffectStats> _catalog = new()
    {
        // プレイテスト調整値：視認性(Alpha)、範囲(Radius)、持続(Lifetime)
        { WorldEffectKind.PoisonCloud, new() { Radius = 100f, Lifetime = 8.0f, Color = Color.FromArgb(160, 100, 200, 50), BlocksVision = true } },
        { WorldEffectKind.DeadlyDome, new() { Radius = 150f, Lifetime = 5.0f, Color = Color.FromArgb(200, 255, 50, 50), BlocksVision = false } },
        { WorldEffectKind.NanoSmoke, new() { Radius = 125f, Lifetime = 14.0f, Color = Color.FromArgb(220, 200, 220, 255), BlocksVision = true } },
        { WorldEffectKind.SilenceZone, new() { Radius = 110f, Lifetime = 10.0f, Color = Color.FromArgb(140, 50, 100, 255), BlocksVision = false } },
        { WorldEffectKind.HunterEye, new() { Radius = 280f, Lifetime = 3.0f, Color = Color.FromArgb(100, 255, 255, 100), BlocksVision = false } },
        { WorldEffectKind.Lockdown, new() { Radius = 240f, Lifetime = 22.0f, Color = Color.FromArgb(180, 255, 200, 0), BlocksVision = false } },
        { WorldEffectKind.SystemCrash, new() { Radius = 600f, Lifetime = 6.0f, Color = Color.FromArgb(120, 150, 50, 255), BlocksVision = false } },
    };

    public static WorldEffectStats Get(WorldEffectKind kind) => _catalog[kind];
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
    public ActorType OwnerType { get; set; } = ActorType.Player;
    public float Health { get; set; }
    public float MaxHealth { get; init; }
    public float PulseCooldown { get; set; }
    public float RemainingLifetime { get; set; }
}

internal sealed class WorldEffect
{
    public required WorldEffectKind Kind { get; init; }
    public required PointF Position { get; init; }
    public required float Radius { get; init; }
    public required float Lifetime { get; init; }
    public required Color Color { get; init; }
    public ActorType OwnerType { get; init; } = ActorType.Player;
    public float Age { get; set; }
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
    public required AgentKind Agent { get; set; }
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
    public float SkillOneCooldown { get; set; }
    public float SkillTwoCooldown { get; set; }
    public float UltimateCharge { get; set; }
    public float AbilityThinkCooldown { get; set; }
    public float DashTimer { get; set; }
    public float OverdriveTimer { get; set; }
    public float HealingTimer { get; set; }
    public float GhostTimer { get; set; }
    public bool IsBoss { get; set; }
    public Queue<PointF> Path { get; } = new();
    public bool IsAlive => Health > 0.01f;
}

internal readonly record struct CosmeticOffer(
    CosmeticKind Kind,
    string Name,
    int TokenCost,
    string Label);
