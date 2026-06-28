#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawCombatTopBar(Graphics graphics)
    {
        var attackersLeft = CurrentAttackerCount();
        var defendersLeft = CurrentDefenderCount();
        var siteState = _bombPlanted ? $"ARMED {CurrentObjectiveSiteLabel()}" : $"SITE {CurrentObjectiveSiteLabel()}";
        var scoreRect = new RectangleF(TopBarBounds.Left + 8f, TopBarBounds.Top + 4f, TopBarBounds.Width - 16f, 16f);
        var footerRect = new RectangleF(TopBarBounds.Left + 8f, TopBarBounds.Top + 20f, TopBarBounds.Width - 16f, 12f);

        DrawCenteredHudText(graphics, $"攻 {attackersLeft}   {_playerRoundWins} - {_enemyRoundWins}   防 {defendersLeft}", 12.4f, FontStyle.Bold, Color.FromArgb(248, 236, 244, 248), scoreRect);
        DrawCenteredHudText(graphics, $"{PlayerRoleLabel()}  |  {siteState}{(_isOvertime ? "  |  OT" : string.Empty)}", 7.2f, FontStyle.Bold, PhaseColor(), footerRect);
    }

    private void DrawBriefingOverlay(Graphics graphics)
    {
        var box = BriefingOverlayBounds;

        using var backdrop = new LinearGradientBrush(box, Color.FromArgb(215, 8, 16, 22), Color.FromArgb(190, 4, 10, 14), 90f);
        using var border = new Pen(Color.FromArgb(120, 90, 215, 230), 2f);
        graphics.FillRectangle(backdrop, box);
        graphics.DrawRectangle(border, box);
        using var accent = new Pen(Color.FromArgb(84, 170, 146, 92), 1.2f);
        graphics.DrawLine(accent, box.Left + 18, box.Top + 40, box.Right - 18, box.Top + 40);

        DrawGhostHudText(graphics, $"死角{360f - DefaultFovDegrees:0}度を、音で視る。", 18.5f, FontStyle.Bold, Color.FromArgb(255, 225, 245, 250), box.Left + 20, box.Top + 14);
        DrawHudText(graphics, "上段で戦況、左右で索敵情報、下段で装備とコマンドを確認できます。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 50);
        DrawHudText(graphics, "前半 4 ラウンドは防衛、後半は攻撃へ切り替わります。攻守交代時には再エディットが入ります。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 72);
        DrawHudText(graphics, $"視界は {DefaultFovDegrees:0} 度。残り {360f - DefaultFovDegrees:0} 度は音の波紋で補完します。投資 300 円付近が最効率で、ボスは同一人物を 2 回まで選出できます。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 94);
        DrawHudText(graphics, "構築中は Tab で拡張設置物、6 でエージェント、Q/E でスキン、R で広告、T でコスメストアを操作できます。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 116);
        DrawHudText(graphics, "Space でこのパネルを閉じます。", 9.8f, FontStyle.Bold, Color.FromArgb(255, 255, 215, 135), box.Left + 20, box.Bottom - 28);
    }

    private void DrawTopBar(Graphics graphics)
    {
        var attackersLeft = CurrentAttackerCount();
        var defendersLeft = CurrentDefenderCount();
        var leftBlock = new RectangleF(TopBarBounds.Left + 10f, TopBarBounds.Top + 6f, (TopBarBounds.Width / 2f) - 14f, 24f);
        var rightBlock = new RectangleF(TopBarBounds.Left + (TopBarBounds.Width / 2f) + 4f, TopBarBounds.Top + 6f, (TopBarBounds.Width / 2f) - 14f, 24f);
        var footer = new RectangleF(TopBarBounds.Left + 8f, TopBarBounds.Bottom - 22f, TopBarBounds.Width - 16f, 14f);

        DrawCenteredHudText(graphics, $"攻 ({attackersLeft})", 18f, FontStyle.Bold, Color.FromArgb(255, 240, 128, 112), leftBlock);
        DrawCenteredHudText(graphics, $"防 ({defendersLeft})", 18f, FontStyle.Bold, Color.FromArgb(255, 120, 236, 218), rightBlock);

        using var divider = new Pen(Color.FromArgb(96, 146, 214, 224), 1.2f);
        graphics.DrawLine(divider, TopBarBounds.Left + (TopBarBounds.Width / 2f), TopBarBounds.Top + 8f, TopBarBounds.Left + (TopBarBounds.Width / 2f), TopBarBounds.Top + 30f);
        var siteState = _bombPlanted ? $"ARMED {CurrentObjectiveSiteLabel()}" : $"SITE {_attackFocusSite switch { ObjectiveSiteId.Alpha => "A", _ => "B" }}";
        DrawCenteredHudText(graphics, $"第{_currentRound}ラウンド  |  {PlayerRoleLabel()}  |  {siteState}  |  SCORE {_playerRoundWins}-{_enemyRoundWins}{(_isOvertime ? " OT" : string.Empty)}  |  {ProfileSummaryLine()}", 8.3f, FontStyle.Bold, Color.FromArgb(236, 214, 224, 232), footer);
    }

    private void DrawRosterPanel(Graphics graphics)
    {
        var total = TeamSize;
        var alive = LiveEnemyCount();
        var columns = 8;
        var iconSize = 22;
        var gap = 8;
        var horizontalPadding = 12;

        DrawHudText(graphics, $"残り人数 {alive}/{total}", 8.4f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), RosterBounds.Left + 10, RosterBounds.Top + 8);
        DrawHudText(graphics, EnemyTeamAttacking() ? "襲撃班" : "守備班", 8f, FontStyle.Bold, Color.FromArgb(255, 240, 128, 112), RosterBounds.Right - 56, RosterBounds.Top + 8);

        for (var index = 0; index < total; index++)
        {
            var row = index / columns;
            var column = index % columns;
            var x = RosterBounds.Left + horizontalPadding + (column * (iconSize + gap));
            var y = RosterBounds.Top + 30 + (row * 26);
            var rect = new Rectangle(x, y, iconSize, iconSize);

            var state = index < alive ? 0 : 2;
            DrawEnemyTrackerPortrait(graphics, rect, state);
        }
    }

    private void DrawIntelPanel(Graphics graphics)
    {
        DrawGhostHudText(graphics, "ターゲット", 10.6f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 6);
        DrawGhostHudText(graphics, CurrentObjectiveTitle(), 11f, FontStyle.Bold, PhaseColor(), IntelBounds.Left + 8, IntelBounds.Top + 30);
        using (var objectiveFont = new Font(UiFontFamily, 8.8f, FontStyle.Regular))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
        using (var objectiveBrush = new SolidBrush(Color.FromArgb(232, 218, 228, 236)))
        {
            var rect = new RectangleF(IntelBounds.Left + 8, IntelBounds.Top + 48, IntelBounds.Width - 16, 44);
            var shadowRect = rect;
            shadowRect.Offset(1f, 1f);
            graphics.DrawString(CurrentObjectiveBody(), objectiveFont, shadowBrush, shadowRect);
            graphics.DrawString(CurrentObjectiveBody(), objectiveFont, objectiveBrush, rect);
        }

        DrawGhostHudText(graphics, "プロフィール", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 102);
        DrawHudText(graphics, ProfileSummaryLine(), 8.4f, FontStyle.Bold, Color.FromArgb(236, 224, 232, 240), IntelBounds.Left + 8, IntelBounds.Top + 124);
        DrawHudText(graphics, ContractSummaryLine(), 8.2f, FontStyle.Regular, Color.FromArgb(220, 198, 212, 222), IntelBounds.Left + 8, IntelBounds.Top + 144);
        DrawHudText(graphics, $"{PlayerAgentProfile().Name} {PlayerAgentProfile().Role} / SKIN {SelectedStructureSkinName()} / AD {SelectedAdThemeName()}", 7.8f, FontStyle.Regular, Color.FromArgb(208, 198, 212, 222), IntelBounds.Left + 8, IntelBounds.Top + 162);
        DrawHudText(graphics, $"{SelectedBannerName()} / KO {SelectedKillEffectName()}", 7.4f, FontStyle.Regular, Color.FromArgb(196, 198, 212, 222), IntelBounds.Left + 8, IntelBounds.Top + 178);

        if (_phase == GamePhase.Bet)
        {
            DrawGhostHudText(graphics, "投資パネル", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 198);

            var investLineY = IntelBounds.Top + 220f;
            foreach (var (_, intelLabel, actorName, accent) in FriendlyBossSlots())
            {
                var selected = _selectedBossName == actorName;
                DrawHudText(graphics, $"{intelLabel}  {GetFriendlyInvestment(actorName)}c  ULT {GetUltPoints(actorName)}/{MaxUltPoints}", 8.2f, selected ? FontStyle.Bold : FontStyle.Regular, selected ? accent : Color.FromArgb(228, 214, 224, 232), IntelBounds.Left + 10, investLineY);
                investLineY += 19f;
            }

            DrawGhostHudText(graphics, "ショップ", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 296);
            DrawHudText(graphics, StoreOfferSummaryLine(), 7.4f, FontStyle.Bold, Color.FromArgb(220, 238, 226, 168), IntelBounds.Left + 8, IntelBounds.Top + 314);
            DrawBetShopList(graphics, new Rectangle(IntelBounds.Left + 8, IntelBounds.Top + 330, IntelBounds.Width - 16, 94));
            return;
        }

        DrawGhostHudText(graphics, "ストア", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 198);
        DrawHudText(graphics, StoreOfferSummaryLine(), 7.4f, FontStyle.Bold, Color.FromArgb(220, 238, 226, 168), IntelBounds.Left + 8, IntelBounds.Top + 218);

        DrawGhostHudText(graphics, "ログ", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 244);
        using var feedFont = new Font(UiFontFamily, 8.4f, FontStyle.Regular);
        using var shadow = new SolidBrush(Color.FromArgb(176, 0, 0, 0));
        using var feedBrush = new SolidBrush(Color.FromArgb(236, 224, 232, 240));
        var lineY = IntelBounds.Top + 270f;
        foreach (var entry in _activityFeed.Take(5))
        {
            var rect = new RectangleF(IntelBounds.Left + 8, lineY, IntelBounds.Width - 16, 28);
            var shadowRect = rect;
            shadowRect.Offset(1f, 1f);
            graphics.DrawString($"- {entry}", feedFont, shadow, shadowRect);
            graphics.DrawString($"- {entry}", feedFont, feedBrush, rect);
            lineY += 24f;
        }
    }
}


#endif
