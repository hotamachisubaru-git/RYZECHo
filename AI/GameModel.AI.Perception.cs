namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Perception — 知覚の要約エントリポイント
    // =========================================================================

    // PickBestTarget / PickEnemyTarget / PickRaycastTarget
    //   → AI.GameModel.AI.Targeting.cs
    // ActorHasDirectSightTo / PlayerHasDirectSightTo / HasLineOfSight / IsVisionBlockedCell
    //   → AI.GameModel.AI.Sight.cs
    // FindPath / Neighbors / IsBlockedCell
    //   → AI.GameModel.AI.Pathfinding.cs
    // ResolveCollision
    //   → AI.GameModel.AI.Sight.cs (Collision)
    // GetAudioOcclusionProfile / PlayerCanPerceive / CountOccludingCells
    //   → AI.GameModel.AI.Sight.cs (Audio)
    // RevealEnemiesInActorVision
    //   → AI.GameModel.AI.Targeting.cs
    // PlayerCanSee
    //   → AI.GameModel.AI.Sight.cs
    // SameTeamSide / IsFriendlyActorType
    //   → AI.GameModel.AI.Sight.cs
    // GetFovDegrees
    //   → AI.GameModel.AI.Sight.cs

    // =========================================================================
    // Legacy — 今後リファクタリング対象
    // =========================================================================

    // このファイルは上記の partial class ファイルにメソッドを分割済み。
    // 残っている場合は旧来の知覚ロジックのエントリポイントとして使用。
}