#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
namespace RYZECHo;

internal sealed partial class GameModel
{
    public void Render(Graphics graphics, Rectangle clientBounds, Point mousePosition)
    {
        _layoutSize = clientBounds.Size;

        if (_hudPhaseLabelTimer > 0f)
        {
            _hudPhaseLabelTimer -= 1f / 60f;
            if (_hudPhaseLabelTimer <= 0f)
            {
                _hudPhaseLabel = "HUNT";
                _hudPhaseLabelTimer = 0f;
            }
        }

        if (_hudKillFeedTimer > 0f)
        {
            _hudKillFeedTimer -= 1f / 60f;
            _hudKillFeed.RemoveAll(entry => entry.SideLabel == "不明");
        }

        if (_hudKillFeed.Count > 8)
        {
            _hudKillFeed.RemoveRange(0, _hudKillFeed.Count - 8);
        }

        if (_hudDamageFlashTimer > 0f)
        {
            _hudDamageFlashTimer -= 1f / 60f;
        }

        SubscribeHudEvents();

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
        DrawKillFeed(graphics);
        DrawDamageFlash(graphics, clientBounds);

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

}
#endif
