#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawMiniMap(Graphics graphics)
    {
        if (_phase != GamePhase.Hunt)
        {
            DrawCenteredHudText(graphics, "全体マップ", 12f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), new RectangleF(MinimapBounds.Left + 10, MinimapBounds.Top + 8, MinimapBounds.Width - 20, 18));
        }

        var inner = Rectangle.Inflate(MinimapBounds, _phase == GamePhase.Hunt ? -8 : -10, _phase == GamePhase.Hunt ? -8 : -10);
        if (_phase != GamePhase.Hunt)
        {
            inner = new Rectangle(inner.Left, inner.Top + 22, inner.Width, inner.Height - 24);
        }

        using var mapBrush = new SolidBrush(Color.FromArgb(188, 10, 16, 22));
        graphics.FillRectangle(mapBrush, inner);

        using (var gridPen = new Pen(Color.FromArgb(26, 98, 228, 242), 1f))
        {
            for (var x = 1; x < 10; x++)
            {
                var xPos = inner.Left + ((inner.Width / 10f) * x);
                graphics.DrawLine(gridPen, xPos, inner.Top, xPos, inner.Bottom);
            }

            for (var y = 1; y < 10; y++)
            {
                var yPos = inner.Top + ((inner.Height / 10f) * y);
                graphics.DrawLine(gridPen, inner.Left, yPos, inner.Right, yPos);
            }
        }

        var viewRect = new RectangleF(WorldBounds.Left, WorldBounds.Top, WorldBounds.Width, WorldBounds.Height);
        var scaleX = inner.Width / viewRect.Width;
        var scaleY = inner.Height / viewRect.Height;

        foreach (var cell in _permanentWalls)
        {
            var rect = CellRectangle(cell);
            if (!viewRect.IntersectsWith(rect))
            {
                continue;
            }

            var miniRect = new RectangleF(
                inner.Left + ((rect.Left - viewRect.Left) * scaleX),
                inner.Top + ((rect.Top - viewRect.Top) * scaleY),
                rect.Width * scaleX,
                rect.Height * scaleY);
            using var wallBrush = new SolidBrush(Color.FromArgb(160, 46, 62, 74));
            graphics.FillRectangle(wallBrush, miniRect);
        }

        foreach (var structure in _structures)
        {
            var center = CellCenter(structure.Cell);
            var color = structure.Kind switch
            {
                StructureKind.BlastDoor => Color.FromArgb(255, 105, 235, 240),
                StructureKind.HoneyTrap => Color.FromArgb(255, 255, 196, 82),
                StructureKind.StaticNest => Color.FromArgb(255, 180, 235, 120),
                StructureKind.ReconBeacon => Color.FromArgb(255, 110, 224, 255),
                StructureKind.ShieldRelay => Color.FromArgb(255, 142, 255, 194),
                StructureKind.PortableCover => Color.FromArgb(255, 214, 224, 236),
                StructureKind.VisorWall => Color.FromArgb(255, 152, 184, 255),
                _ => Color.FromArgb(255, 196, 132, 255),
            };

            using var brush = new SolidBrush(color);
            if (!viewRect.Contains(center))
            {
                continue;
            }

            var point = new PointF(inner.Left + ((center.X - viewRect.Left) * scaleX), inner.Top + ((center.Y - viewRect.Top) * scaleY));
            graphics.FillEllipse(brush, point.X - 3.5f, point.Y - 3.5f, 7f, 7f);
        }

        DrawMiniMapCameraFootprint(graphics, inner, viewRect, scaleX, scaleY);

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            DrawMiniMapFovCone(graphics, inner, viewRect, scaleX, scaleY, ally, Color.FromArgb(34, 124, 214, 255), 0.44f);
        }

        DrawMiniMapFovCone(graphics, inner, viewRect, scaleX, scaleY, _player, Color.FromArgb(56, 98, 228, 242), 0.55f);

        DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, _player, Color.FromArgb(255, 90, 225, 245));
        foreach (var ally in _allies)
        {
            DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, ally, Color.FromArgb(255, 95, 225, 200));
        }

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && (PlayerCanSee(actor) || IsEnemySharedVisible(actor))))
        {
            DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, enemy, Color.FromArgb(255, 235, 105, 90));
        }

        var playerPoint = new PointF(inner.Left + ((_player.Position.X - viewRect.Left) * scaleX), inner.Top + ((_player.Position.Y - viewRect.Top) * scaleY));
        var pulseSize = 12f + (2.5f * (0.5f + (0.5f * MathF.Sin(_uiPulseTime * 5.8f))));
        using (var pingPen = new Pen(Color.FromArgb(148, 98, 228, 242), 1.4f))
        {
            graphics.DrawEllipse(pingPen, playerPoint.X - 18f, playerPoint.Y - 18f, 36f, 36f);
            graphics.DrawEllipse(pingPen, playerPoint.X - 32f, playerPoint.Y - 32f, 64f, 64f);
            graphics.DrawEllipse(pingPen, playerPoint.X - pulseSize, playerPoint.Y - pulseSize, pulseSize * 2f, pulseSize * 2f);
        }
        using (var playerBrush = new SolidBrush(Color.FromArgb(255, 90, 225, 245)))
        using (var centerPen = new Pen(Color.FromArgb(244, 236, 244, 248), 1.4f))
        {
            graphics.FillEllipse(playerBrush, playerPoint.X - 4.5f, playerPoint.Y - 4.5f, 9f, 9f);
            graphics.DrawLine(centerPen, playerPoint.X - 8f, playerPoint.Y, playerPoint.X + 8f, playerPoint.Y);
            graphics.DrawLine(centerPen, playerPoint.X, playerPoint.Y - 8f, playerPoint.X, playerPoint.Y + 8f);
        }

        foreach (var site in GetBombSites())
        {
            var core = BombSitePosition(site.Id);
            using var coreBrush = new SolidBrush(_bombPlanted && _armedBombSiteId == site.Id ? Color.FromArgb(255, 255, 128, 106) : site.Id == _attackFocusSite ? Color.FromArgb(255, 78, 220, 195) : Color.FromArgb(210, 92, 174, 188));
            if (viewRect.Contains(core))
            {
                var corePoint = new PointF(inner.Left + ((core.X - viewRect.Left) * scaleX), inner.Top + ((core.Y - viewRect.Top) * scaleY));
                graphics.FillEllipse(coreBrush, corePoint.X - 5f, corePoint.Y - 5f, 10f, 10f);
                DrawHudText(graphics, site.Label, 7f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), corePoint.X + 6f, corePoint.Y - 8f);
            }
        }

        using var border = new Pen(Color.FromArgb(146, 194, 170, 110), 2.2f);
        graphics.DrawRectangle(border, inner);
    }

    private void DrawMiniMapFovCone(Graphics graphics, Rectangle inner, RectangleF viewRect, float scaleX, float scaleY, Actor actor, Color color, float rangeScale)
    {
        if (_phase != GamePhase.Hunt || !actor.IsAlive || !viewRect.Contains(actor.Position))
        {
            return;
        }

        var point = MiniMapPoint(inner, viewRect, scaleX, scaleY, actor.Position);
        var fovRadius = Math.Clamp(_weaponStats[actor.Weapon].VisionRange * scaleX * rangeScale, 10f, inner.Width * 0.48f);
        var fovDegrees = GetFovDegrees(actor.Weapon);
        var startAngle = RadiansToDegrees(actor.FacingAngle) - (fovDegrees / 2f);

        using var fovPath = new GraphicsPath();
        using var coneBrush = new SolidBrush(color);
        fovPath.AddPie(point.X - fovRadius, point.Y - fovRadius, fovRadius * 2f, fovRadius * 2f, startAngle, fovDegrees);
        graphics.FillPath(coneBrush, fovPath);
    }

    private void DrawMiniMapCameraFootprint(Graphics graphics, Rectangle inner, RectangleF viewRect, float scaleX, float scaleY)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return;
        }

        var cameraPoints = GetActiveCameraWorldCorners()
            .Select(point => MiniMapPoint(inner, viewRect, scaleX, scaleY, point))
            .ToArray();

        using var fill = new SolidBrush(Color.FromArgb(28, 92, 228, 242));
        using var border = new Pen(Color.FromArgb(210, 126, 236, 248), 1.8f);
        using var glow = new Pen(Color.FromArgb(86, 126, 236, 248), 3.6f);
        graphics.FillPolygon(fill, cameraPoints);
        graphics.DrawPolygon(glow, cameraPoints);
        graphics.DrawPolygon(border, cameraPoints);
    }

    private static PointF MiniMapPoint(Rectangle inner, RectangleF viewRect, float scaleX, float scaleY, PointF worldPoint)
    {
        var x = inner.Left + ((worldPoint.X - viewRect.Left) * scaleX);
        var y = inner.Top + ((worldPoint.Y - viewRect.Top) * scaleY);
        return new PointF(
            Math.Clamp(x, inner.Left, inner.Right),
            Math.Clamp(y, inner.Top, inner.Bottom));
    }
}


#endif
