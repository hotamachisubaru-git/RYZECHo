namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawCombatFog(Graphics graphics)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return;
        }

        var expandedWorld = RectangleF.Inflate(WorldBounds, CellSize * 2.5f, CellSize * 2.5f);
        using var fog = new SolidBrush(Color.FromArgb(158, 2, 7, 12));
        graphics.FillRectangle(fog, expandedWorld);

        var weapon = _weaponStats[_player.Weapon];
        var range = weapon.VisionRange;
        var fovDegrees = GetFovDegrees(_player.Weapon);
        var startAngle = RadiansToDegrees(_player.FacingAngle) - (fovDegrees / 2f);
        var diameter = range * 2f;

        using var visionPath = new GraphicsPath();
        visionPath.AddPie(_player.Position.X - range, _player.Position.Y - range, diameter, diameter, startAngle, fovDegrees);

        using var visibleFill = new SolidBrush(Color.FromArgb(48, 244, 236, 188));
        using var visibleEdge = new Pen(Color.FromArgb(190, 244, 232, 172), 1.8f);
        using var outerEdge = new Pen(Color.FromArgb(90, 88, 232, 248), 1.4f);
        graphics.FillPath(visibleFill, visionPath);
        graphics.DrawPath(visibleEdge, visionPath);
        graphics.DrawEllipse(outerEdge, _player.Position.X - range, _player.Position.Y - range, diameter, diameter);

        DrawDeadAngleBoundary(graphics, startAngle, range);
        DrawDeadAngleBoundary(graphics, startAngle + fovDegrees, range);
    }

    private void DrawDeadAngleBoundary(Graphics graphics, float degrees, float range)
    {
        var radians = DegreesToRadians(degrees);
        var end = new PointF(
            _player.Position.X + (MathF.Cos(radians) * range),
            _player.Position.Y + (MathF.Sin(radians) * range));

        using var pen = new Pen(Color.FromArgb(132, 248, 224, 156), 1.2f);
        graphics.DrawLine(pen, _player.Position, end);
    }

    private void DrawCombatScreenVignette(Graphics graphics, Rectangle clientBounds)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return;
        }

        var sideWidth = (int)Math.Clamp(clientBounds.Width * 0.14f, 64f, 220f);
        var topHeight = (int)Math.Clamp(clientBounds.Height * 0.10f, 42f, 120f);
        var bottomHeight = (int)Math.Clamp(clientBounds.Height * 0.14f, 68f, 160f);

        using var sideBrush = new SolidBrush(Color.FromArgb(96, 0, 0, 0));
        using var topBrush = new SolidBrush(Color.FromArgb(78, 0, 0, 0));
        using var bottomBrush = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
        using var visorPen = new Pen(Color.FromArgb(110, 78, 224, 238), 1.6f);

        graphics.FillRectangle(sideBrush, clientBounds.Left, clientBounds.Top, sideWidth, clientBounds.Height);
        graphics.FillRectangle(sideBrush, clientBounds.Right - sideWidth, clientBounds.Top, sideWidth, clientBounds.Height);
        graphics.FillRectangle(topBrush, clientBounds.Left, clientBounds.Top, clientBounds.Width, topHeight);
        graphics.FillRectangle(bottomBrush, clientBounds.Left, clientBounds.Bottom - bottomHeight, clientBounds.Width, bottomHeight);

        var leftGuide = clientBounds.Left + sideWidth;
        var rightGuide = clientBounds.Right - sideWidth;
        graphics.DrawLine(visorPen, leftGuide, clientBounds.Top + topHeight, leftGuide, clientBounds.Bottom - bottomHeight);
        graphics.DrawLine(visorPen, rightGuide, clientBounds.Top + topHeight, rightGuide, clientBounds.Bottom - bottomHeight);
    }
}
