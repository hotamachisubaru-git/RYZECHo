namespace RYZECHo.Prototype;

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
        var rows = (int)Math.Ceiling(total / (float)columns);
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

    private void DrawBottomBar(Graphics graphics)
    {
        if (_phase == GamePhase.Hunt)
        {
            DrawCombatBottomBar(graphics);
            return;
        }

        DrawChampionHudFrame(graphics, BottomHudBounds);

        var statusRect = new Rectangle(BottomHudBounds.Left + 16, BottomHudBounds.Top + 10, 236, 82);
        var skillRects = new[]
        {
            new Rectangle(statusRect.Right + 18, BottomHudBounds.Top + 18, 62, 58),
            new Rectangle(statusRect.Right + 88, BottomHudBounds.Top + 18, 62, 58),
            new Rectangle(statusRect.Right + 158, BottomHudBounds.Top + 18, 62, 58),
        };
        var abilityRect = new Rectangle(skillRects[2].Right + 14, BottomHudBounds.Top + 18, 72, 58);
        var mainWeaponRect = new Rectangle(BottomHudBounds.Right - 202, BottomHudBounds.Top + 18, 58, 58);
        var subWeaponRect = new Rectangle(mainWeaponRect.Right + 6, BottomHudBounds.Top + 18, 58, 58);
        var knifeRect = new Rectangle(subWeaponRect.Right + 6, BottomHudBounds.Top + 18, 58, 58);
        var footerRect = new Rectangle(BottomHudBounds.Left + 16, BottomHudBounds.Bottom - 28, BottomHudBounds.Width - 32, 16);

        DrawInsetPanel(graphics, statusRect);
        foreach (var rect in skillRects)
        {
            DrawInsetPanel(graphics, rect);
        }

        DrawInsetPanel(graphics, abilityRect);
        DrawInsetPanel(graphics, mainWeaponRect);
        DrawInsetPanel(graphics, subWeaponRect);
        DrawInsetPanel(graphics, knifeRect);

        DrawHudText(graphics, CurrentModeTitle(), 9.8f, FontStyle.Bold, PhaseColor(), statusRect.Left + 10, statusRect.Top + 8);
        var hpBar = new RectangleF(statusRect.Left + 10, statusRect.Top + 28, statusRect.Width - 20, 10);
        var shieldBar = new RectangleF(statusRect.Left + 10, statusRect.Top + 44, statusRect.Width - 20, 10);
        var sonicBar = new RectangleF(statusRect.Left + 10, statusRect.Top + 60, statusRect.Width - 20, 9);
        if (_phase == GamePhase.Bet)
        {
            var investDenominator = Math.Max(OptimalBossInvestment * 2, Math.Max(1, AffordableCredits() + _selectedBet));
            DrawLabeledBar(graphics, hpBar, "総投資", _selectedBet / (float)investDenominator, Color.FromArgb(255, 238, 202, 112), Color.FromArgb(36, 8, 14, 18), $"{_selectedBet}c");
            DrawLabeledBar(graphics, shieldBar, "ボス投資", Math.Clamp(SelectedBossInvestment() / (float)OptimalBossInvestment, 0f, 1f), Color.FromArgb(255, 92, 168, 232), Color.FromArgb(36, 8, 14, 18), $"{SelectedBossInvestment()}c");
            DrawLabeledBar(graphics, sonicBar, "ULT", SelectedBossUltPoints() / (float)MaxUltPoints, Color.FromArgb(255, 120, 214, 160), Color.FromArgb(36, 8, 14, 18), $"{SelectedBossUltPoints()}/{MaxUltPoints}");
        }
        else
        {
            DrawLabeledBar(graphics, hpBar, "体力", _player.Health / _player.MaxHealth, Color.FromArgb(255, 88, 196, 88), Color.FromArgb(36, 8, 14, 18), $"{(int)_player.Health}");
            DrawLabeledBar(graphics, shieldBar, "シールド", _player.MaxShield <= 0f ? 0f : _player.Shield / _player.MaxShield, Color.FromArgb(255, 92, 168, 232), Color.FromArgb(36, 8, 14, 18), $"{(int)_player.Shield}");
            DrawLabeledBar(graphics, sonicBar, "SONIC", _weaponStats[DisplayedWeaponType()].HearingMultiplier / 1.25f, Color.FromArgb(255, 74, 186, 232), Color.FromArgb(36, 8, 14, 18), $"{_weaponStats[DisplayedWeaponType()].HearingMultiplier:0.0}x");
        }

        if (_phase == GamePhase.Construct)
        {
            DrawAbilitySlot(graphics, skillRects[0], "1", "スキル1", "防壁", _selectedBuildTool == BuildToolKind.BlastDoor, Color.FromArgb(255, 116, 212, 230), _buildPoints / 2, Math.Clamp(_buildPoints / 2f, 0f, 1f), _buildPoints >= 2);
            DrawAbilitySlot(graphics, skillRects[1], "2", "スキル2", "蜜罠", _selectedBuildTool == BuildToolKind.HoneyTrap, Color.FromArgb(255, 230, 194, 88), _buildPoints / 3, Math.Clamp(_buildPoints / 3f, 0f, 1f), _buildPoints >= 3);
            DrawAbilitySlot(graphics, skillRects[2], "3", "スキル3", "静巣", _selectedBuildTool == BuildToolKind.StaticNest, Color.FromArgb(255, 164, 220, 116), _buildPoints / 4, Math.Clamp(_buildPoints / 4f, 0f, 1f), _buildPoints >= 4);
            var advancedSelected = _selectedBuildTool is BuildToolKind.PortableCover or BuildToolKind.VisorWall or BuildToolKind.HoloDecoy;
            DrawAbilitySlot(graphics, abilityRect, advancedSelected ? "TAB" : "4", advancedSelected ? "拡張" : "スキル4", advancedSelected ? BuildToolShortLabel(_selectedBuildTool) : "索敵", _selectedBuildTool == BuildToolKind.ReconBeacon || advancedSelected, Color.FromArgb(255, 124, 228, 255), _buildPoints / Math.Max(1, BuildToolApCost(_selectedBuildTool)), Math.Clamp(_buildPoints / (float)Math.Max(1, BuildToolApCost(_selectedBuildTool)), 0f, 1f), _buildPoints >= BuildToolApCost(_selectedBuildTool));
        }
        else if (_phase == GamePhase.Bet)
        {
            var bossSlots = FriendlyBossSlots();
            DrawBossInvestmentSlot(graphics, skillRects[0], bossSlots[0]);
            DrawBossInvestmentSlot(graphics, skillRects[1], bossSlots[1]);
            DrawBossInvestmentSlot(graphics, skillRects[2], bossSlots[2]);
            DrawBossInvestmentSlot(graphics, abilityRect, bossSlots[3]);
        }
        else
        {
            var weapon = _weaponStats[_player.Weapon];
            var fireCharge = 1f - Math.Clamp(_player.FireCooldown / MathF.Max(0.01f, GetActorFireCooldown(_player, weapon.FireCooldown)), 0f, 1f);
            var interactRatio = _bombPlanted
                ? Math.Clamp(_bombDefuseProgress / BombDefuseSeconds, 0f, 1f)
                : Math.Clamp(_bombPlantProgress / BombPlantSeconds, 0f, 1f);
            var interactReady = !_bombPlanted
                ? (!IsPlayerTeamAttacking() || (_player.IsAlive && IsInsideBombSite(_player.Position, 10f)))
                : (!IsPlayerTeamAttacking() && CanPlayerDefuse());
            DrawAbilitySlot(graphics, skillRects[0], _player.Weapon == _playerPrimaryWeapon ? "Q" : "E", weapon.ShortLabel, $"{weapon.VisionClass}視界", false, WeaponAccent(weapon.Type), 1, Math.Clamp(weapon.VisionRange / 500f, 0f, 1f), true);
            DrawAbilitySlot(graphics, skillRects[1], "MAG", "弾数", $"{weapon.MagazineAmmo}/{weapon.ReserveAmmo}", false, Color.FromArgb(255, 116, 212, 230), 1, fireCharge, fireCharge >= 0.995f);
            DrawAbilitySlot(graphics, skillRects[2], "ULT", "ボス", _player.IsBoss ? $"K {_roundBossKillCount} / U{SelectedBossUltPoints()}" : "非ボス", _player.IsBoss, Color.FromArgb(255, 164, 220, 116), _player.IsBoss ? Math.Max(1, SelectedBossUltPoints()) : 0, BossInvestmentProgress(CurrentBossInvestment(_player)), _player.IsBoss);
            DrawAbilitySlot(graphics, abilityRect, IsPlayerTeamAttacking() && _bombPlanted ? "-" : "F", "アクション", CurrentSiteActionLabel(), false, Color.FromArgb(255, 208, 170, 104), 1, interactRatio, interactReady);
        }

        if (_phase == GamePhase.Construct)
        {
            DrawLoadoutBox(graphics, mainWeaponRect, "5", "RELAY");
            DrawLoadoutBox(graphics, subWeaponRect, "選択", BuildToolShortLabel(_selectedBuildTool));
            DrawLoadoutBox(graphics, knifeRect, "AP", _buildPoints.ToString());
        }
        else
        {
            DrawLoadoutBox(graphics, mainWeaponRect, "メイン", WeaponLoadoutLabel(_phase == GamePhase.Bet ? _selectedWeapon : _playerPrimaryWeapon));
            DrawLoadoutBox(graphics, subWeaponRect, "サブ", WeaponLoadoutLabel(_phase == GamePhase.Bet ? _selectedSidearmWeapon : _playerSidearmWeapon));
            DrawLoadoutBox(graphics, knifeRect, "サイト", CurrentObjectiveSiteLabel());
        }
        DrawCenteredHudText(graphics, CurrentControlsHint(), 7.6f, FontStyle.Bold, Color.FromArgb(234, 214, 224, 232), footerRect);
    }

    private void DrawCombatBottomBar(Graphics graphics)
    {
        DrawChampionHudFrame(graphics, BottomHudBounds);

        var portraitCenter = new PointF(BottomHudBounds.Left + 56f, BottomHudBounds.Top + 52f);
        var weapon = _weaponStats[_player.Weapon];
        var fireCharge = 1f - Math.Clamp(_player.FireCooldown / MathF.Max(0.01f, GetActorFireCooldown(_player, weapon.FireCooldown)), 0f, 1f);
        var interactRatio = _bombPlanted
            ? Math.Clamp(_bombDefuseProgress / BombDefuseSeconds, 0f, 1f)
            : Math.Clamp(_bombPlantProgress / BombPlantSeconds, 0f, 1f);
        var interactReady = !_bombPlanted
            ? (!IsPlayerTeamAttacking() || (_player.IsAlive && IsInsideBombSite(_player.Position, 10f)))
            : (!IsPlayerTeamAttacking() && CanPlayerDefuse());

        DrawPortraitOrb(graphics, portraitCenter, 78f, _player.IsBoss ? Color.FromArgb(255, 218, 178, 84) : Color.FromArgb(255, 54, 172, 198));

        var statusRect = new Rectangle(BottomHudBounds.Left + 100, BottomHudBounds.Top + 18, 158, 64);
        DrawInsetPanel(graphics, statusRect);
        DrawHudText(graphics, _player.IsBoss ? $"{PlayerAgentProfile().Name} BOSS" : PlayerAgentProfile().Name, 8.4f, FontStyle.Bold, PhaseColor(), statusRect.Left + 8, statusRect.Top + 5);
        DrawLabeledBar(graphics, new RectangleF(statusRect.Left + 8, statusRect.Top + 22, statusRect.Width - 16, 8), "HP", _player.Health / _player.MaxHealth, Color.FromArgb(255, 76, 194, 104), Color.FromArgb(42, 6, 12, 14), $"{(int)_player.Health}");
        DrawLabeledBar(graphics, new RectangleF(statusRect.Left + 8, statusRect.Top + 37, statusRect.Width - 16, 8), "SHD", _player.MaxShield <= 0f ? 0f : _player.Shield / _player.MaxShield, Color.FromArgb(255, 70, 144, 224), Color.FromArgb(42, 6, 12, 14), $"{(int)_player.Shield}");
        DrawLabeledBar(graphics, new RectangleF(statusRect.Left + 8, statusRect.Top + 52, statusRect.Width - 16, 7), "SONIC", weapon.HearingMultiplier / 1.25f, Color.FromArgb(255, 74, 186, 232), Color.FromArgb(42, 6, 12, 14), $"{weapon.HearingMultiplier:0.0}x");

        var skillRects = new[]
        {
            new Rectangle(BottomHudBounds.Left + 278, BottomHudBounds.Top + 24, 56, 54),
            new Rectangle(BottomHudBounds.Left + 342, BottomHudBounds.Top + 24, 56, 54),
            new Rectangle(BottomHudBounds.Left + 406, BottomHudBounds.Top + 24, 56, 54),
            new Rectangle(BottomHudBounds.Left + 470, BottomHudBounds.Top + 24, 64, 54),
        };

        foreach (var rect in skillRects)
        {
            DrawInsetPanel(graphics, rect);
        }

        var agent = PlayerAgentProfile();
        DrawAbilitySlot(graphics, skillRects[0], "1", agent.SkillOne, AgentSkillCooldown(AgentAbilitySlot.SkillOne) > 0f ? $"{AgentSkillCooldown(AgentAbilitySlot.SkillOne):0.0}s" : "READY", false, agent.Accent, AgentAbilityReady(AgentAbilitySlot.SkillOne) ? 1 : 0, AgentAbilityProgress(AgentAbilitySlot.SkillOne), AgentAbilityReady(AgentAbilitySlot.SkillOne));
        DrawAbilitySlot(graphics, skillRects[1], "2", agent.SkillTwo, AgentSkillCooldown(AgentAbilitySlot.SkillTwo) > 0f ? $"{AgentSkillCooldown(AgentAbilitySlot.SkillTwo):0.0}s" : "READY", false, agent.Accent, AgentAbilityReady(AgentAbilitySlot.SkillTwo) ? 1 : 0, AgentAbilityProgress(AgentAbilitySlot.SkillTwo), AgentAbilityReady(AgentAbilitySlot.SkillTwo));
        DrawAbilitySlot(graphics, skillRects[2], "3", "ULT", agent.Ultimate, AgentAbilityReady(AgentAbilitySlot.Ultimate), agent.Accent, GetUltPoints(_player.Name), AgentAbilityProgress(AgentAbilitySlot.Ultimate), AgentAbilityReady(AgentAbilitySlot.Ultimate));
        DrawAbilitySlot(graphics, skillRects[3], IsPlayerTeamAttacking() && _bombPlanted ? "-" : "F", "ACT", CurrentSiteActionLabel(), false, Color.FromArgb(255, 208, 170, 104), 1, interactRatio, interactReady);

        var loadoutRects = new[]
        {
            new Rectangle(BottomHudBounds.Left + 552, BottomHudBounds.Top + 24, 48, 54),
            new Rectangle(BottomHudBounds.Left + 606, BottomHudBounds.Top + 24, 48, 54),
        };

        foreach (var rect in loadoutRects)
        {
            DrawInsetPanel(graphics, rect);
        }

        DrawLoadoutBox(graphics, loadoutRects[0], "MAIN", WeaponLoadoutLabel(_playerPrimaryWeapon));
        DrawLoadoutBox(graphics, loadoutRects[1], "SUB", WeaponLoadoutLabel(_playerSidearmWeapon));
    }

    private void DrawInfoStatBox(Graphics graphics, Rectangle bounds, string title, string value, Color valueColor)
    {
        DrawCenteredHudText(graphics, title, 7.4f, FontStyle.Bold, Color.FromArgb(228, 214, 224, 232), new RectangleF(bounds.Left + 2, bounds.Top + 4, bounds.Width - 4, 10));
        DrawCenteredHudText(graphics, value, 9.8f, FontStyle.Bold, valueColor, new RectangleF(bounds.Left + 2, bounds.Top + 13, bounds.Width - 4, bounds.Height - 14));
    }

    private static (string Hotkey, string IntelLabel, string ActorName, Color Accent)[] FriendlyBossSlots()
    {
        return
        [
            ("1", "1 あなた", RosterCatalog.PlayerName, Color.FromArgb(255, 116, 212, 230)),
            ("2", "2 北班", RosterCatalog.NorthAnchorName, Color.FromArgb(255, 164, 220, 116)),
            ("3", "3 南班", RosterCatalog.SouthAnchorName, Color.FromArgb(255, 230, 194, 88)),
            ("4", "4 中班", RosterCatalog.CenterLinkName, Color.FromArgb(255, 208, 170, 104)),
        ];
    }

    private void DrawBossInvestmentSlot(
        Graphics graphics,
        Rectangle bounds,
        (string Hotkey, string IntelLabel, string ActorName, Color Accent) slot)
    {
        DrawAbilitySlot(
            graphics,
            bounds,
            slot.Hotkey,
            "投資枠",
            $"{GetFriendlyInvestment(slot.ActorName)}c / U{GetUltPoints(slot.ActorName)}",
            _selectedBossName == slot.ActorName,
            slot.Accent,
            BossSelectionsRemaining(slot.ActorName),
            Math.Clamp(GetFriendlyInvestment(slot.ActorName) / (float)OptimalBossInvestment, 0f, 1f),
            CanSelectBoss(slot.ActorName));
    }

}
