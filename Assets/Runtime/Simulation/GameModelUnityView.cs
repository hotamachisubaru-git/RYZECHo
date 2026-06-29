namespace RYZECHo;

public sealed partial class GameModel
{
    public PointF PlayerModelPosition => _player.Position;
    public float PlayerFacingRadians => _player.FacingAngle;
    public float PlayerFovDegrees => GetFovDegrees(_player.Weapon);
    public float PlayerVisionRange => _weaponStats[_player.Weapon].VisionRange;
    public float ModelCellSize => CellSize;

    public PointF ModelToCellSpace(PointF modelPosition)
    {
        return new PointF(
            (modelPosition.X - WorldBounds.Left) / CellSize,
            (WorldBounds.Bottom - modelPosition.Y) / CellSize);
    }
}
