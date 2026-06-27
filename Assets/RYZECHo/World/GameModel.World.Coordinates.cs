namespace RYZECHo;

internal sealed partial class GameModel
{
    private void EmitRipple(PointF position, float strength, RippleKind kind, Color color)
    {
        _ripples.Add(new Ripple
        {
            Position = position,
            Strength = strength,
            Lifetime = SoundCueLifetimeSeconds,
            Kind = kind,
            Color = color,
        });

        _eventBus.Emit(new AudioCueEvent(kind, position, strength));
    }

    private Point ScreenToCell(Point location)
    {
        var worldPoint = ScreenToWorldPoint(location);
        return new Point(
            (int)MathF.Floor((worldPoint.X - WorldBounds.Left) / CellSize),
            (int)MathF.Floor((worldPoint.Y - WorldBounds.Top) / CellSize));
    }

    private bool TryGetWorldPointFromScreen(Point screenPoint, out PointF worldPoint)
    {
        worldPoint = ScreenToWorldPoint(screenPoint);
        return worldPoint.X >= WorldBounds.Left &&
               worldPoint.X < WorldBounds.Right &&
               worldPoint.Y >= WorldBounds.Top &&
               worldPoint.Y < WorldBounds.Bottom;
    }

    private PointF ScreenToWorldPoint(Point screenPoint)
    {
        var points = new[] { new PointF(screenPoint.X, screenPoint.Y) };
        using var projection = CreateActiveWorldMatrix();
        projection.Invert();
        projection.TransformPoints(points);
        return points[0];
    }

    private Point WorldToCell(PointF point)
    {
        var x = (int)Math.Clamp((point.X - WorldBounds.Left) / CellSize, 0f, GridColumns - 1);
        var y = (int)Math.Clamp((point.Y - WorldBounds.Top) / CellSize, 0f, GridRows - 1);
        return new Point(x, y);
    }

    private Rectangle CellRectangle(Point cell)
    {
        return new Rectangle(WorldBounds.Left + (cell.X * CellSize), WorldBounds.Top + (cell.Y * CellSize), CellSize, CellSize);
    }

    private PointF CellCenter(Point cell)
    {
        return new PointF(
            WorldBounds.Left + (cell.X * CellSize) + (CellSize / 2f),
            WorldBounds.Top + (cell.Y * CellSize) + (CellSize / 2f));
    }

    private PointF CorePosition()
    {
        return CellCenter(new Point(14, 6));
    }

    private Point GetBombSiteCell()
    {
        return GetBombSite(CurrentObjectiveSiteId()).Cell;
    }

    private PointF BombSitePosition()
    {
        return BombSitePosition(CurrentObjectiveSiteId());
    }

    private PointF BombSitePosition(ObjectiveSiteId siteId)
    {
        return CellCenter(GetBombSite(siteId).Cell);
    }

    private static Point MirrorCellHorizontally(Point cell)
    {
        return new Point((GridColumns - 1) - cell.X, cell.Y);
    }

    private Matrix CreateActiveWorldMatrix()
    {
        var matrix = CreateWorldProjectionMatrix();
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return matrix;
        }

        var focusPoints = new[] { new PointF(_player.Position.X, _player.Position.Y) };
        matrix.TransformPoints(focusPoints);
        var focusScreen = focusPoints[0];
        var cameraBounds = MainPlayCameraBounds;
        var targetScreen = new PointF(
            cameraBounds.Left + (cameraBounds.Width * HuntCameraTargetX),
            cameraBounds.Top + (cameraBounds.Height * HuntCameraTargetY));

        var zoom = ActiveHuntCameraZoom(cameraBounds);
        matrix.Translate(-focusScreen.X, -focusScreen.Y, MatrixOrder.Append);
        matrix.Scale(zoom, zoom, MatrixOrder.Append);
        matrix.Translate(focusScreen.X, focusScreen.Y, MatrixOrder.Append);
        matrix.Translate(targetScreen.X - focusScreen.X, targetScreen.Y - focusScreen.Y, MatrixOrder.Append);
        return matrix;
    }

    private float ActiveHuntCameraZoom(RectangleF cameraBounds)
    {
        var zoomForWidth = cameraBounds.Width / MathF.Max(1f, WorldVisualBounds.Width * HuntVisibleWorldFractionX);
        var zoomForHeight = cameraBounds.Height / MathF.Max(1f, WorldVisualBounds.Height * HuntVisibleWorldFractionY);
        return MathF.Max(HuntCameraZoom, MathF.Max(zoomForWidth, zoomForHeight));
    }

    private Matrix CreateWorldProjectionMatrix()
    {
        return new Matrix(
            WorldPerspectiveScaleX,
            0f,
            WorldPerspectiveShearX,
            WorldPerspectiveScaleY,
            WorldVisualBounds.Left - (WorldPerspectiveScaleX * WorldBounds.Left) - (WorldPerspectiveShearX * WorldBounds.Top),
            WorldVisualBounds.Top - (WorldPerspectiveScaleY * WorldBounds.Top));
    }

    private PointF[] GetProjectedWorldCorners()
    {
        var points = new[]
        {
            new PointF(WorldBounds.Left, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Bottom),
            new PointF(WorldBounds.Left, WorldBounds.Bottom),
        };

        using var projection = CreateActiveWorldMatrix();
        projection.TransformPoints(points);
        return points;
    }

    private PointF[] GetActiveCameraWorldCorners()
    {
        var cameraBounds = MainPlayCameraBounds;
        var points = new[]
        {
            new PointF(cameraBounds.Left, cameraBounds.Top),
            new PointF(cameraBounds.Right, cameraBounds.Top),
            new PointF(cameraBounds.Right, cameraBounds.Bottom),
            new PointF(cameraBounds.Left, cameraBounds.Bottom),
        };

        using var projection = CreateActiveWorldMatrix();
        projection.Invert();
        projection.TransformPoints(points);
        return points;
    }

    private string BuildToolLabel(BuildToolKind tool)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => $"髦ｲ螢√ラ繧｢ / {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.HoneyTrap => $"繝上メ繝溘ヤ繝医Λ繝・・ / {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.StaticNest => $"繧ｹ繧ｿ繝・ぅ繝・け繝阪せ繝・/ {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.ReconBeacon => $"繝ｪ繧ｳ繝ｳ繝薙・繧ｳ繝ｳ / {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.ShieldRelay => $"繧ｷ繝ｼ繝ｫ繝峨Μ繝ｬ繝ｼ / {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.PortableCover => $"繝昴・繧ｿ繝悶Ν繧ｫ繝舌・ / {MapEditApRules.ToolApCost(tool)}AP",
            BuildToolKind.VisorWall => $"荳譁ｹ蜷代ヰ繧､繧ｶ繝ｼ螢・/ {MapEditApRules.ToolApCost(tool)}AP",
            _ => $"繝帙Ο繧ｰ繝ｩ繝繝・さ繧､ / {MapEditApRules.ToolApCost(tool)}AP",
        };
    }

    private static string BuildToolShortLabel(BuildToolKind tool)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => "DOOR",
            BuildToolKind.HoneyTrap => "HONEY",
            BuildToolKind.StaticNest => "NEST",
            BuildToolKind.ReconBeacon => "RECON",
            BuildToolKind.ShieldRelay => "RELAY",
            BuildToolKind.PortableCover => "COVER",
            BuildToolKind.VisorWall => "VISOR",
            _ => "DECOY",
        };
    }

    private static int BuildToolApCost(BuildToolKind tool)
    {
        return MapEditApRules.ToolApCost(tool);
    }

    private string PhaseLabel()
    {
        return _phase switch
        {
            GamePhase.Construct => "讒狗ｯ・,
            GamePhase.Bet => "謚戊ｳ・,
            GamePhase.Hunt => IsPlayerTeamAttacking() ? "謾ｻ謦・ : "髦ｲ陦・,
            GamePhase.RoundResult => "邊ｾ邂・,
            GamePhase.Victory => "蜍晏茜",
            _ => "謨怜圏",
        };
    }

    private Color PhaseColor()
    {
        return _phase switch
        {
            GamePhase.Construct => Color.FromArgb(255, 115, 225, 205),
            GamePhase.Bet => Color.FromArgb(255, 255, 225, 130),
            GamePhase.Hunt => Color.FromArgb(255, 255, 125, 105),
            GamePhase.Victory => Color.FromArgb(255, 120, 235, 165),
            GamePhase.Defeat => Color.FromArgb(255, 255, 105, 95),
            _ => Color.FromArgb(255, 205, 215, 225),
        };
    }

    private static float Distance(PointF left, PointF right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    private static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.PI * 2f;
        }

        while (angle < -MathF.PI)
        {
            angle += MathF.PI * 2f;
        }

        return angle;
    }
}
