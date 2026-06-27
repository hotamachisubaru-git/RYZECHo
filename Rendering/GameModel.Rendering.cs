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

        if (IsPaused)
        {
            DrawPauseOverlay(graphics, clientBounds);
        }

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

    private void DrawBriefingOverlay(Graphics graphics)
    {
        if (!_showBriefing || _briefingTitle == null || _briefingBody == null)
        {
            return;
        }

        var bounds = BriefingOverlayBounds;
        using var overlayFill = new SolidBrush(Color.FromArgb(178, 12, 18, 28));
        using var overlayBorder = new Pen(Color.FromArgb(180, 110, 150, 178), 1.8f);
        using var overlayGlow = new Pen(Color.FromArgb(72, 92, 200, 248), 2.4f);
        graphics.FillRectangle(overlayFill, bounds);
        graphics.DrawRectangle(overlayBorder, bounds);
        graphics.DrawRectangle(overlayGlow, bounds.Left - 3, bounds.Top - 3, bounds.Width + 6, bounds.Height + 6);

        using var titleBrush = new SolidBrush(Color.FromArgb(248, 242, 224, 202));
        using var bodyBrush = new SolidBrush(Color.FromArgb(238, 216, 226, 236));
        using var accentBrush = new SolidBrush(Color.FromArgb(220, 134, 224, 255));
        using var titleFont = new Font("Segoe UI", 13.4f, FontStyle.Bold, GraphicsUnit.Point);
        using var bodyFont = new Font("Segoe UI", 10.2f, FontStyle.Regular, GraphicsUnit.Point);
        using var accentFont = new Font("Segoe UI", 9.4f, FontStyle.Italic, GraphicsUnit.Point);

        graphics.DrawString(_briefingTitle, titleFont, titleBrush, bounds.Left + 14f, bounds.Top + 12f);
        graphics.DrawString(_briefingBody, bodyFont, bodyBrush, bounds.Left + 14f, bounds.Top + 40f);
        graphics.DrawString(_briefingSub, accentFont, accentBrush, bounds.Left + 14f, bounds.Top + 72f);

        var hintY = bounds.Top + 108f;
        using var hintBrush = new SolidBrush(Color.FromArgb(170, 130, 162, 186));
        using var hintFont = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point);
        graphics.DrawString("Enter で次へ", hintFont, hintBrush, bounds.Left + 14f, hintY);
    }

    private void DrawKillFeed(Graphics graphics)
    {
        if (_hudKillFeedTimer <= 0f || _hudKillFeed.Count == 0)
        {
            return;
        }

        var alpha = (int)(Math.Clamp(_hudKillFeedTimer / 0.6f, 0f, 1f) * 232f);
        var baseY = TopBarBounds.Top - 12f;
        var index = 0;
        foreach (var entry in _hudKillFeed)
        {
            var entryAlpha = alpha;
            var entryColor = entry.Side == KillFeedSide.Attacker
                ? Color.FromArgb(entryAlpha, 255, 226, 112)
                : Color.FromArgb(entryAlpha, 255, 132, 108);

            var text = $"{entry.KillerName} → {entry.VictimName}";
            var textBounds = new RectangleF(KillFeedBounds.Left + 14f, baseY + (index * 22f), KillFeedBounds.Width - 28f, 20f);
            DrawCenteredHudText(graphics, text, 8.4f, FontStyle.Bold, entryColor, textBounds);
            index++;
        }
    }

    private void DrawDamageFlash(Graphics graphics, Rectangle clientBounds)
    {
        if (_hudDamageFlashTimer <= 0f)
        {
            return;
        }

        var intensity = Math.Clamp(_hudDamageFlashTimer / 0.6f, 0f, 1f);
        var flashColor = Color.FromArgb((int)(intensity * 52), _hudDamageFlashColor);
        using var flashBrush = new SolidBrush(flashColor);
        graphics.FillRectangle(flashBrush, clientBounds);
    }

}
