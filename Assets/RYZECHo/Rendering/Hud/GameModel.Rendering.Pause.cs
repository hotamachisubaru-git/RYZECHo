#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
namespace RYZECHo;

internal sealed partial class GameModel
{
    private bool _pauseMouseOverResume = false;
    private Rectangle _pauseResumeButtonBounds;

    private void DrawPauseOverlay(Graphics graphics, Rectangle clientBounds)
    {
        // Dim backdrop
        using var backdrop = new SolidBrush(Color.FromArgb(180, 4, 8, 14));
        graphics.FillRectangle(backdrop, clientBounds);

        var centerX = clientBounds.Left + (clientBounds.Width / 2f);
        var centerY = clientBounds.Top + (clientBounds.Height / 2f);

        // Title
        DrawCenteredHudText(
            graphics,
            "一時停止",
            42f,
            FontStyle.Bold,
            Color.FromArgb(255, 230, 240, 248),
            new RectangleF(centerX - 180, centerY - 160, 360, 56));

        // Subtitle
        DrawCenteredHudText(
            graphics,
            "ESC で再開 / 下のボタンでも再開できます",
            12f,
            FontStyle.Regular,
            Color.FromArgb(180, 180, 200, 210),
            new RectangleF(centerX - 200, centerY - 100, 400, 24));

        // Resume button
        var btnWidth = 200f;
        var btnHeight = 44f;
        var btnX = centerX - (btnWidth / 2f);
        var btnY = centerY - 30f;
        _pauseResumeButtonBounds = new Rectangle((int)btnX, (int)btnY, (int)btnWidth, (int)btnHeight);

        var btnColor = _pauseMouseOverResume
            ? Color.FromArgb(255, 60, 140, 255)
            : Color.FromArgb(255, 30, 70, 130);
        var btnBorderColor = _pauseMouseOverResume
            ? Color.FromArgb(255, 140, 200, 255)
            : Color.FromArgb(255, 80, 140, 200);

        using var btnFill = new SolidBrush(btnColor);
        graphics.FillRectangle(btnFill, btnX, btnY, btnWidth, btnHeight);

        using var btnBorder = new Pen(btnBorderColor, 2f);
        graphics.DrawRectangle(btnBorder, btnX, btnY, btnWidth, btnHeight);

        using var btnGlow = _pauseMouseOverResume
            ? new SolidBrush(Color.FromArgb(40, 60, 140, 255))
            : new SolidBrush(Color.FromArgb(20, 30, 70, 130));
        graphics.FillRectangle(btnGlow, btnX - 2, btnY - 2, btnWidth + 4, btnHeight + 4);

        // Button text
        DrawCenteredHudText(
            graphics,
            "再開 (ESC)",
            14f,
            FontStyle.Bold,
            Color.FromArgb(255, 240, 245, 250),
            new RectangleF(btnX + 8, btnY + 2, btnWidth - 16, btnHeight - 4));

        // Phase info
        var phaseText = PhaseLabel();
        var scoreText = $"SCORE {_playerRoundWins} - {_enemyRoundWins}";
        var roundText = $"第{_currentRound}ラウンド";

        var infoY = centerY + 40f;
        DrawCenteredHudText(
            graphics,
            $"{roundText}  |  状態: {phaseText}",
            11f,
            FontStyle.Regular,
            Color.FromArgb(160, 180, 200, 214),
            new RectangleF(centerX - 220, infoY, 440, 22));

        // Credits
        var creditY = infoY + 26f;
        DrawCenteredHudText(
            graphics,
            $"所持金: {_credits} 円",
            11f,
            FontStyle.Regular,
            Color.FromArgb(160, 238, 202, 112),
            new RectangleF(centerX - 100, creditY, 200, 22));
    }

    internal void UpdatePauseMouse(Point mousePosition)
    {
        _pauseMouseOverResume = _pauseResumeButtonBounds.Contains(mousePosition);
    }

    internal void HandlePauseResume()
    {
        IsPaused = false;
    }
}
#endif
