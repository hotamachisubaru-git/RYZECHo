#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
    private Rectangle WorldBounds => new((((_layoutSize.Width - (GridColumns * CellSize)) / 2) - 96), 88, GridColumns * CellSize, GridRows * CellSize);

    private Rectangle TopBarBounds => _phase == GamePhase.Hunt
        ? new((_layoutSize.Width - 320) / 2, 8, 320, 36)
        : new((_layoutSize.Width - 340) / 2, 16, 340, TopBarHeight);

    private Rectangle BottomHudBounds => _phase == GamePhase.Hunt
        ? new((_layoutSize.Width - 680) / 2, _layoutSize.Height - 116, 680, 100)
        : new((_layoutSize.Width - 780) / 2, _layoutSize.Height - BottomHudHeight - 18, 780, BottomHudHeight);

    private Rectangle SidePanelBounds => new(_layoutSize.Width - WorldMargin - SidePanelWidth, WorldBounds.Top, SidePanelWidth, WorldBounds.Height);

    private Rectangle RosterBounds => new(_layoutSize.Width - WorldMargin - SidePanelWidth, 18, SidePanelWidth, 88);

    private Rectangle IntelBounds => new(
        _layoutSize.Width - WorldMargin - SidePanelWidth,
        RosterBounds.Bottom + 8,
        SidePanelWidth,
        332);

    private Rectangle MinimapBounds => _phase == GamePhase.Hunt
        ? new(_layoutSize.Width - 198, _layoutSize.Height - 174, 174, 150)
        : new(WorldMargin, 18, 176, 140);

    private Rectangle TimerBounds => _phase == GamePhase.Hunt
        ? new(_layoutSize.Width - 116, 8, 100, 28)
        : new(MinimapBounds.Left, MinimapBounds.Bottom + 6, 104, 30);

    private Rectangle CreditsBounds => _phase == GamePhase.Hunt
        ? new(TimerBounds.Left - 108, 8, 100, 28)
        : new(TimerBounds.Right + 4, MinimapBounds.Bottom + 6, MinimapBounds.Right - (TimerBounds.Right + 4), 30);

    private Rectangle BriefingOverlayBounds
    {
        get
        {
            var width = Math.Min(760, _layoutSize.Width - (WorldMargin * 2));
            var height = 188;
            var top = Math.Max(MinimapBounds.Bottom + 18, BottomHudBounds.Top - height - 16);
            return new Rectangle((_layoutSize.Width - width) / 2, top, width, height);
        }
    }

    
    private Rectangle KillFeedBounds
    {
        get
        {
            var width = 280;
            var height = 200;
            var top = TopBarBounds.Top - height - 4;
            var left = TopBarBounds.Left;
            return new Rectangle(left, top, width, height);
        }
    }

    private RectangleF MainPlayCameraBounds
    {
        get
        {
            if (_phase == GamePhase.Hunt)
            {
                return new RectangleF(8f, 8f, Math.Max(360f, _layoutSize.Width - 16f), Math.Max(260f, _layoutSize.Height - 16f));
            }

            var top = TopBarBounds.Bottom + 12f;
            var bottom = BottomHudBounds.Top - 12f;
            var right = RosterBounds.Left - SidePanelGap;
            var width = Math.Max(360f, right - WorldMargin);
            var height = Math.Max(260f, bottom - top);
            return new RectangleF(WorldMargin, top, width, height);
        }
    }

    private RectangleF WorldVisualBounds => new(
        WorldBounds.Left + ((WorldBounds.Width - ((WorldBounds.Width * WorldPerspectiveScaleX) + (WorldBounds.Height * WorldPerspectiveShearX))) / 2f),
        WorldBounds.Top + WorldPerspectiveTopInset,
        (WorldBounds.Width * WorldPerspectiveScaleX) + (WorldBounds.Height * WorldPerspectiveShearX),
        WorldBounds.Height * WorldPerspectiveScaleY);
}


#endif
