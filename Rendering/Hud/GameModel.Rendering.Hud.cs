#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawHud(Graphics graphics)
    {
        if (_phase == GamePhase.Hunt)
        {
            DrawPanelFrame(graphics, TopBarBounds);
            DrawPanelFrame(graphics, TimerBounds);
            DrawPanelFrame(graphics, CreditsBounds);
            DrawPanelFrame(graphics, MinimapBounds);
            DrawPanelFrame(graphics, BottomHudBounds);

            DrawCombatTopBar(graphics);
            DrawMiniMap(graphics);
            DrawInfoStatBox(graphics, TimerBounds, "TIME", $"{Math.Max(0f, _roundTimer):0.0}", PhaseColor());
            DrawInfoStatBox(graphics, CreditsBounds, "GOLD", _credits.ToString(), Color.FromArgb(255, 238, 202, 112));
            DrawBottomBar(graphics);
            DrawSoundEdgeIndicators(graphics);
            return;
        }

        DrawPanelFrame(graphics, TopBarBounds);
        DrawPanelFrame(graphics, RosterBounds);
        DrawPanelFrame(graphics, MinimapBounds);
        DrawPanelFrame(graphics, TimerBounds);
        DrawPanelFrame(graphics, CreditsBounds);
        DrawPanelFrame(graphics, BottomHudBounds);

        DrawTopBar(graphics);
        DrawMiniMap(graphics);
        DrawInfoStatBox(graphics, TimerBounds, TimerLabel(), _phase == GamePhase.Hunt ? $"{Math.Max(0f, _roundTimer):0.0}s" : PhaseLabel(), PhaseColor());
        DrawInfoStatBox(graphics, CreditsBounds, "所持金", _credits.ToString(), Color.FromArgb(255, 238, 202, 112));
        DrawRosterPanel(graphics);
        DrawIntelPanel(graphics);
        DrawBottomBar(graphics);
        DrawSoundEdgeIndicators(graphics);
    }

    private void DrawInfoStatBox(Graphics graphics, Rectangle bounds, string title, string value, Color valueColor)
    {
        DrawCenteredHudText(graphics, title, 7.4f, FontStyle.Bold, Color.FromArgb(228, 214, 224, 232), new RectangleF(bounds.Left + 2, bounds.Top + 4, bounds.Width - 4, 10));
        DrawCenteredHudText(graphics, value, 9.8f, FontStyle.Bold, valueColor, new RectangleF(bounds.Left + 2, bounds.Top + 13, bounds.Width - 4, bounds.Height - 14));
    }
}


#endif
