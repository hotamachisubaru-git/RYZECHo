namespace RYZECHo;

internal sealed partial class GameModel
{
    internal PointF PlayerModelPosition => _player.Position;
    internal float PlayerFacingRadians => _player.FacingAngle;
    internal float PlayerFovDegrees => GetFovDegrees(_player.Weapon);
    internal float PlayerVisionRange => _weaponStats[_player.Weapon].VisionRange;
    internal float ModelCellSize => CellSize;

    internal PointF ModelToCellSpace(PointF modelPosition)
    {
        return new PointF(
            (modelPosition.X - WorldBounds.Left) / CellSize,
            (WorldBounds.Bottom - modelPosition.Y) / CellSize);
    }
}

