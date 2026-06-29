#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
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
            DrawLabeledBar(graphics, shieldBar, "ボス投資", BossInvestmentProgress(SelectedBossInvestment()), Color.FromArgb(255, 92, 168, 232), Color.FromArgb(36, 8, 14, 18), $"{SelectedBossInvestment()}c");
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
            DrawConstructionSlots(graphics, skillRects, abilityRect);
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
            DrawRoundActionSlots(graphics, skillRects, abilityRect);
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

    private void DrawConstructionSlots(Graphics graphics, Rectangle[] skillRects, Rectangle abilityRect)
    {
        var blastDoorCost = BuildToolApCost(BuildToolKind.BlastDoor);
        var honeyTrapCost = BuildToolApCost(BuildToolKind.HoneyTrap);
        var staticNestCost = BuildToolApCost(BuildToolKind.StaticNest);
        DrawAbilitySlot(graphics, skillRects[0], "1", "スキル1", "防壁", _selectedBuildTool == BuildToolKind.BlastDoor, Color.FromArgb(255, 116, 212, 230), _buildPoints / Math.Max(1, blastDoorCost), Math.Clamp(_buildPoints / (float)Math.Max(1, blastDoorCost), 0f, 1f), _buildPoints >= blastDoorCost);
        DrawAbilitySlot(graphics, skillRects[1], "2", "スキル2", "蜜罠", _selectedBuildTool == BuildToolKind.HoneyTrap, Color.FromArgb(255, 230, 194, 88), _buildPoints / Math.Max(1, honeyTrapCost), Math.Clamp(_buildPoints / (float)Math.Max(1, honeyTrapCost), 0f, 1f), _buildPoints >= honeyTrapCost);
        DrawAbilitySlot(graphics, skillRects[2], "3", "スキル3", "静巣", _selectedBuildTool == BuildToolKind.StaticNest, Color.FromArgb(255, 164, 220, 116), _buildPoints / Math.Max(1, staticNestCost), Math.Clamp(_buildPoints / (float)Math.Max(1, staticNestCost), 0f, 1f), _buildPoints >= staticNestCost);
        var advancedSelected = _selectedBuildTool is BuildToolKind.PortableCover or BuildToolKind.VisorWall or BuildToolKind.HoloDecoy;
        DrawAbilitySlot(graphics, abilityRect, advancedSelected ? "TAB" : "4", advancedSelected ? "拡張" : "スキル4", advancedSelected ? BuildToolShortLabel(_selectedBuildTool) : "索敵", _selectedBuildTool == BuildToolKind.ReconBeacon || advancedSelected, Color.FromArgb(255, 124, 228, 255), _buildPoints / Math.Max(1, BuildToolApCost(_selectedBuildTool)), Math.Clamp(_buildPoints / (float)Math.Max(1, BuildToolApCost(_selectedBuildTool)), 0f, 1f), _buildPoints >= BuildToolApCost(_selectedBuildTool));
    }

    private void DrawRoundActionSlots(Graphics graphics, Rectangle[] skillRects, Rectangle abilityRect)
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
            BossInvestmentProgress(GetFriendlyInvestment(slot.ActorName)),
            CanSelectBoss(slot.ActorName));
    }
}


#endif
