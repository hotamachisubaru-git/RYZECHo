namespace RYZECHo;

internal sealed partial class GameModel
{
    private Structure CreateStructure(BuildToolKind tool, Point cell)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => new Structure
            {
                Kind = StructureKind.BlastDoor,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "防壁ドア",
                Health = 120f,
                MaxHealth = 120f,
                PulseCooldown = 0f,
            },
            BuildToolKind.HoneyTrap => new Structure
            {
                Kind = StructureKind.HoneyTrap,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "ハチミツトラップ",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0f,
            },
            BuildToolKind.StaticNest => new Structure
            {
                Kind = StructureKind.StaticNest,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "スタティックネスト",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.3f,
            },
            BuildToolKind.ReconBeacon => new Structure
            {
                Kind = StructureKind.ReconBeacon,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "リコンビーコン",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.45f,
            },
            BuildToolKind.ShieldRelay => new Structure
            {
                Kind = StructureKind.ShieldRelay,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "シールドリレー",
                Health = 90f,
                MaxHealth = 90f,
                PulseCooldown = 0.6f,
            },
            BuildToolKind.PortableCover => new Structure
            {
                Kind = StructureKind.PortableCover,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "ポータブルカバー",
                Health = 70f,
                MaxHealth = 70f,
                PulseCooldown = 0f,
            },
            BuildToolKind.VisorWall => new Structure
            {
                Kind = StructureKind.VisorWall,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "一方向バイザー壁",
                Health = 55f,
                MaxHealth = 55f,
                PulseCooldown = 0.8f,
            },
            _ => new Structure
            {
                Kind = StructureKind.HoloDecoy,
                Cell = cell,
                APCost = MapEditApRules.ToolApCost(tool),
                Label = "ホログラムデコイ",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.7f,
            },
        };
    }

}
