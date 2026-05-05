namespace RYZECHo.Prototype;

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

        matrix.Translate(-focusScreen.X, -focusScreen.Y, MatrixOrder.Append);
        matrix.Scale(HuntCameraZoom, HuntCameraZoom, MatrixOrder.Append);
        matrix.Translate(focusScreen.X, focusScreen.Y, MatrixOrder.Append);
        matrix.Translate(targetScreen.X - focusScreen.X, targetScreen.Y - focusScreen.Y, MatrixOrder.Append);
        return matrix;
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
            BuildToolKind.BlastDoor => "防壁ドア / 2AP",
            BuildToolKind.HoneyTrap => "ハチミツトラップ / 3AP",
            BuildToolKind.StaticNest => "スタティックネスト / 4AP",
            BuildToolKind.ReconBeacon => "リコンビーコン / 4AP",
            _ => "シールドリレー / 5AP",
        };
    }

    private string PhaseLabel()
    {
        return _phase switch
        {
            GamePhase.Construct => "構築",
            GamePhase.Bet => "投資",
            GamePhase.Hunt => IsPlayerTeamAttacking() ? "攻撃" : "防衛",
            GamePhase.RoundResult => "精算",
            GamePhase.Victory => "勝利",
            _ => "敗北",
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
