namespace RYZECHo;

internal sealed partial class GameModel
{
    public void Render(Graphics graphics, Rectangle clientBounds, Point mousePosition)
    {
        _layoutSize = clientBounds.Size;

        using var background = new LinearGradientBrush(clientBounds, Color.FromArgb(7, 14, 22), Color.FromArgb(3, 8, 14), 90f);
        graphics.FillRectangle(background, clientBounds);
        using var vignette = new LinearGradientBrush(clientBounds, Color.FromArgb(0, 86, 229, 247), Color.FromArgb(26, 20, 54, 84), 22f);
        graphics.FillRectangle(vignette, clientBounds);

        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            DrawWorldDropShadow(graphics);
        }

        var worldMousePosition = ScreenToWorldPoint(mousePosition);
        var graphicsState = graphics.Save();
        using (var worldTransform = CreateActiveWorldMatrix())
        {
            graphics.MultiplyTransform(worldTransform);
            DrawWorldPanel(graphics);
            DrawWorldEffects(graphics);
            DrawStructures(graphics);
            DrawCore(graphics);
            DrawCombatFog(graphics);
            DrawRipples(graphics);
            DrawActors(graphics, worldMousePosition);
        }

        graphics.Restore(graphicsState);
        DrawCombatScreenVignette(graphics, clientBounds);
        DrawHud(graphics);

        if (_showBriefing)
        {
            DrawBriefingOverlay(graphics);
        }
    }

    private void DrawWorldDropShadow(Graphics graphics)
    {
        var corners = GetProjectedWorldCorners();
        var shadow = corners.Select(point => new PointF(point.X + 18f, point.Y + 20f)).ToArray();
        using var shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
        graphics.FillPolygon(shadowBrush, shadow);
    }

}
