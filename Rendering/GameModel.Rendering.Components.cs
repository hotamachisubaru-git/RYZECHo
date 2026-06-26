namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawHudText(Graphics graphics, string text, float size, FontStyle style, Color color, float x, float y)
    {
        using var font = new Font(UiFontFamily, size, style);
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, x, y);
    }

    private void DrawGhostHudText(Graphics graphics, string text, float size, FontStyle style, Color color, float x, float y)
    {
        DrawHudText(graphics, text, size, style, Color.FromArgb(178, 0, 0, 0), x + 1f, y + 1f);
        DrawHudText(graphics, text, size, style, color, x, y);
    }

    private void DrawCenteredHudText(Graphics graphics, string text, float size, FontStyle style, Color color, RectangleF bounds)
    {
        using var font = new Font(UiFontFamily, size, style);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
        };

        graphics.DrawString(text, font, brush, bounds, format);
    }

    private void DrawChampionHudFrame(Graphics graphics, Rectangle bounds)
    {
        using var outerBrush = new LinearGradientBrush(bounds, Color.FromArgb(188, 18, 30, 42), Color.FromArgb(168, 10, 16, 22), 90f);
        graphics.FillRectangle(outerBrush, bounds);

        using var goldBorder = new Pen(Color.FromArgb(214, 170, 146, 92), 2.4f);
        graphics.DrawRectangle(goldBorder, bounds);

        using var innerBorder = new Pen(Color.FromArgb(105, 80, 116, 132), 1.2f);
        graphics.DrawRectangle(innerBorder, Rectangle.Inflate(bounds, -8, -8));

        var crest = new[]
        {
            new Point(bounds.Left + 18, bounds.Top),
            new Point(bounds.Left + 46, bounds.Top - 14),
            new Point(bounds.Left + 78, bounds.Top),
        };

        using var crestBrush = new SolidBrush(Color.FromArgb(214, 170, 146, 92));
        graphics.FillPolygon(crestBrush, crest);
        graphics.FillPolygon(crestBrush, crest.Select(point => new Point(bounds.Right - (point.X - bounds.Left), point.Y)).ToArray());
    }

    private void DrawPortraitOrb(Graphics graphics, PointF center, float diameter, Color accent)
    {
        var outerRect = new RectangleF(center.X - (diameter / 2f), center.Y - (diameter / 2f), diameter, diameter);
        var innerRect = RectangleF.Inflate(outerRect, -10f, -10f);

        using var shadow = new SolidBrush(Color.FromArgb(72, 0, 0, 0));
        graphics.FillEllipse(shadow, outerRect.X + 8f, outerRect.Y + 10f, outerRect.Width, outerRect.Height);

        using var rimBrush = new LinearGradientBrush(Rectangle.Round(outerRect), Color.FromArgb(220, 176, 152, 94), Color.FromArgb(150, 84, 60, 34), 90f);
        using var coreBrush = new LinearGradientBrush(Rectangle.Round(innerRect), accent, Color.FromArgb(255, 26, 52, 72), 90f);
        using var rimPen = new Pen(Color.FromArgb(244, 212, 184, 122), 2.4f);
        using var innerPen = new Pen(Color.FromArgb(120, 222, 240, 248), 1.4f);
        graphics.FillEllipse(rimBrush, outerRect);
        graphics.FillEllipse(coreBrush, innerRect);
        graphics.DrawEllipse(rimPen, outerRect);
        graphics.DrawEllipse(innerPen, innerRect);

        using var emblemPen = new Pen(Color.FromArgb(210, 236, 244, 248), 2f);
        graphics.DrawLine(emblemPen, center.X - 18f, center.Y, center.X + 18f, center.Y);
        graphics.DrawLine(emblemPen, center.X, center.Y - 18f, center.X, center.Y + 18f);
        graphics.DrawEllipse(emblemPen, center.X - 10f, center.Y - 10f, 20f, 20f);
    }

    private void DrawLabeledBar(Graphics graphics, RectangleF bounds, string label, float ratio, Color fillColor, Color backColor, string valueText)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);

        using var backBrush = new SolidBrush(backColor);
        using var fillBrush = new SolidBrush(fillColor);
        using var borderPen = new Pen(Color.FromArgb(148, 170, 146, 92), 1.2f);
        graphics.FillRectangle(backBrush, bounds);
        graphics.FillRectangle(fillBrush, bounds.Left, bounds.Top, bounds.Width * ratio, bounds.Height);
        graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

        DrawHudText(graphics, label, 7.2f, FontStyle.Bold, Color.FromArgb(236, 240, 232, 214), bounds.Left + 4f, bounds.Top - 1f);
        DrawHudText(graphics, valueText, 7.4f, FontStyle.Bold, Color.FromArgb(236, 240, 232, 214), bounds.Right - 48f, bounds.Top - 1f);
    }

    private void DrawAbilitySlot(Graphics graphics, Rectangle bounds, string hotkey, string title, string subtitle, bool selected, Color accent, int charges, float chargeRatio, bool ready)
    {
        chargeRatio = Math.Clamp(chargeRatio, 0f, 1f);
        var glow = ready ? 0.72f + (0.28f * (0.5f + (0.5f * MathF.Sin(_uiPulseTime * 6.4f)))) : 0f;
        var accentFill = ready ? Color.FromArgb((int)(120 + (44 * glow)), accent) : Color.FromArgb(82, 70, 76, 82);
        using var fill = new LinearGradientBrush(bounds, selected ? accentFill : Color.FromArgb(82, 70, 76, 82), Color.FromArgb(68, 16, 20, 24), 90f);
        using var border = new Pen(ready ? Color.FromArgb((int)(172 + (48 * glow)), accent) : Color.FromArgb(122, 154, 154, 154), selected || ready ? 2f : 1.2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);

        if (chargeRatio < 0.999f)
        {
            using var cooldownFill = new SolidBrush(Color.FromArgb(150, 10, 12, 16));
            var coverHeight = (bounds.Height - 2) * (1f - chargeRatio);
            graphics.FillRectangle(cooldownFill, bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, coverHeight);
        }

        using (var chargeFill = new SolidBrush(ready ? Color.FromArgb(210, accent) : Color.FromArgb(160, 116, 126, 138)))
        {
            graphics.FillRectangle(chargeFill, bounds.Left + 2, bounds.Bottom - 5, (bounds.Width - 4) * chargeRatio, 3f);
        }

        DrawHudText(graphics, hotkey, 8f, FontStyle.Bold, Color.FromArgb(246, 244, 228, 196), bounds.Left + 6, bounds.Top + 4);
        DrawCenteredHudText(graphics, title, 9f, FontStyle.Bold, Color.FromArgb(242, 238, 244, 248), new RectangleF(bounds.Left + 4, bounds.Top + 18, bounds.Width - 8, 14));
        DrawCenteredHudText(graphics, subtitle, 7.4f, FontStyle.Regular, Color.FromArgb(226, 208, 220, 228), new RectangleF(bounds.Left + 4, bounds.Top + 34, bounds.Width - 8, 14));
        DrawHudText(graphics, $"x{Math.Max(0, charges)}", 7.2f, FontStyle.Bold, ready ? Color.FromArgb(246, 238, 244, 248) : Color.FromArgb(204, 182, 188, 196), bounds.Right - 20, bounds.Bottom - 16);
    }

    private void DrawItemSlot(Graphics graphics, Rectangle bounds, string label, Color accent, bool selected)
    {
        using var fill = new SolidBrush(selected ? Color.FromArgb(132, accent) : Color.FromArgb(92, 18, 26, 32));
        using var border = new Pen(selected ? Color.FromArgb(240, accent) : Color.FromArgb(116, 154, 138, 102), selected ? 2f : 1.2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        DrawCenteredHudText(graphics, label, 8.2f, FontStyle.Bold, Color.FromArgb(246, 238, 244, 248), new RectangleF(bounds.Left + 2, bounds.Top + 2, bounds.Width - 4, bounds.Height - 4));
    }

    private void DrawLoadoutBox(Graphics graphics, Rectangle bounds, string title, string value)
    {
        DrawCenteredHudText(graphics, title, 7.4f, FontStyle.Bold, Color.FromArgb(236, 214, 224, 232), new RectangleF(bounds.Left + 2, bounds.Top + 4, bounds.Width - 4, 10));
        DrawCenteredHudText(graphics, value, 7.6f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), new RectangleF(bounds.Left + 2, bounds.Top + 18, bounds.Width - 4, bounds.Height - 20));
    }

    private void DrawStatusEffects(Graphics graphics, Actor actor, PointF origin)
    {
        var offsetY = 0f;
        if (IsActorOnHoneyTrap(actor))
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "鈍足", Color.FromArgb(255, 240, 188, 92));
            offsetY -= 16f;
        }

        if (IsActorInStaticField(actor))
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "妨害", Color.FromArgb(255, 136, 226, 140));
            offsetY -= 16f;
        }

        if (actor.Type == ActorType.Enemy && IsActorLockedDown(actor))
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "封鎖", Color.FromArgb(255, 230, 194, 88));
            offsetY -= 16f;
        }

        if (actor.Type == ActorType.Player && IsPlayerBreathingExposed())
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "呼吸漏", Color.FromArgb(255, 255, 132, 108));
            offsetY -= 16f;
        }

        if (actor.Type == ActorType.Player && _playerOverdriveTimer > 0f)
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "OD", Color.FromArgb(255, 255, 132, 92));
            offsetY -= 16f;
        }

        if (actor.Type == ActorType.Player && _playerGhostTimer > 0f)
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "幽歩", Color.FromArgb(255, 196, 132, 255));
        }
    }

    private void DrawEffectTag(Graphics graphics, PointF center, string text, Color accent)
    {
        var width = 44f;
        var rect = new RectangleF(center.X - (width / 2f), center.Y, width, 14f);
        using var fill = new SolidBrush(Color.FromArgb(170, 8, 12, 18));
        using var border = new Pen(Color.FromArgb(188, accent), 1f);
        graphics.FillRectangle(fill, rect.X, rect.Y, rect.Width, rect.Height);
        graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
        DrawCenteredHudText(graphics, text, 7.2f, FontStyle.Bold, Color.FromArgb(246, 238, 244, 248), rect);
    }

    private void DrawEnemyTrackerPortrait(Graphics graphics, Rectangle bounds, int state)
    {
        var accent = state switch
        {
            0 => Color.FromArgb(255, 240, 128, 112),
            1 => Color.FromArgb(160, 120, 132, 146),
            _ => Color.FromArgb(120, 74, 80, 88),
        };
        using var bodyBrush = new SolidBrush(Color.FromArgb(state == 0 ? 212 : 108, accent));
        using var ringPen = new Pen(Color.FromArgb(state == 0 ? 228 : 128, accent), 1.2f);
        var head = new RectangleF(bounds.Left + 6f, bounds.Top + 2f, bounds.Width - 12f, bounds.Height * 0.44f);
        var torso = new RectangleF(bounds.Left + 4f, bounds.Top + 11f, bounds.Width - 8f, bounds.Height - 13f);
        graphics.FillEllipse(bodyBrush, head);
        graphics.FillEllipse(bodyBrush, torso);
        graphics.DrawEllipse(ringPen, bounds);

        if (state == 2)
        {
            using var crossPen = new Pen(Color.FromArgb(220, 246, 238, 244), 1.8f);
            graphics.DrawLine(crossPen, bounds.Left + 4, bounds.Top + 4, bounds.Right - 4, bounds.Bottom - 4);
            graphics.DrawLine(crossPen, bounds.Right - 4, bounds.Top + 4, bounds.Left + 4, bounds.Bottom - 4);
        }
    }

    private void DrawWeaponStatusCard(Graphics graphics, Rectangle bounds)
    {
        var weapon = _weaponStats[DisplayedWeaponType()];
        using var weaponPen = new Pen(Color.FromArgb(228, 214, 188, 118), 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        var midY = bounds.Top + (bounds.Height / 2f);
        graphics.DrawLine(weaponPen, bounds.Left + 16f, midY, bounds.Left + 104f, midY);
        graphics.DrawLine(weaponPen, bounds.Left + 34f, midY - 8f, bounds.Left + 58f, midY - 8f);
        graphics.DrawLine(weaponPen, bounds.Left + 76f, midY, bounds.Left + 92f, midY - 10f);
        graphics.DrawLine(weaponPen, bounds.Left + 92f, midY - 10f, bounds.Left + 110f, midY - 10f);
        graphics.DrawLine(weaponPen, bounds.Left + 96f, midY, bounds.Left + 118f, midY + 6f);

        DrawHudText(graphics, weapon.Code, 7.8f, FontStyle.Bold, Color.FromArgb(236, 238, 244, 248), bounds.Left + 122, bounds.Top + 5);
        DrawHudText(graphics, $"{weapon.MagazineAmmo}/{weapon.ReserveAmmo}", 9.2f, FontStyle.Bold, Color.FromArgb(248, 238, 244, 248), bounds.Right - 72, bounds.Top + 4);
        DrawHudText(graphics, $"{weapon.VisionClass}視界 / {weapon.Category}", 7.4f, FontStyle.Bold, Color.FromArgb(236, 164, 232, 168), bounds.Left + 12, bounds.Bottom - 14);
    }

    private void DrawQuickStatusStrip(Graphics graphics, Rectangle bounds)
    {
        var chipBounds = new[]
        {
            new Rectangle(bounds.Left + 8, bounds.Top + 3, 28, bounds.Height - 6),
            new Rectangle(bounds.Left + 40, bounds.Top + 3, 28, bounds.Height - 6),
            new Rectangle(bounds.Left + 72, bounds.Top + 3, 34, bounds.Height - 6),
        };

        DrawItemSlot(graphics, chipBounds[0], "R", Color.FromArgb(255, 116, 212, 230), _phase == GamePhase.RoundResult);
        DrawItemSlot(graphics, chipBounds[1], "C", Color.FromArgb(255, 214, 190, 108), _phase == GamePhase.Construct);
        DrawItemSlot(graphics, chipBounds[2], "3", Color.FromArgb(255, 220, 170, 92), false);

        DrawHudText(graphics, CurrentControlsHint(), 7.7f, FontStyle.Bold, Color.FromArgb(238, 214, 224, 232), bounds.Left + 118, bounds.Top + 8);
    }

    private void DrawPanelFrame(Graphics graphics, Rectangle bounds)
    {
        using var fill = new LinearGradientBrush(bounds, Color.FromArgb(118, 14, 20, 28), Color.FromArgb(92, 8, 12, 18), 90f);
        using var border = new Pen(Color.FromArgb(144, 168, 150, 104), 1.6f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        using var inner = new Pen(Color.FromArgb(54, 108, 126, 138), 1f);
        graphics.DrawRectangle(inner, Rectangle.Inflate(bounds, -6, -6));
    }

    private void DrawInsetPanel(Graphics graphics, Rectangle bounds)
    {
        using var fill = new LinearGradientBrush(bounds, Color.FromArgb(94, 22, 28, 34), Color.FromArgb(70, 12, 16, 20), 90f);
        using var border = new Pen(Color.FromArgb(112, 154, 154, 154), 1.2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        using var inner = new Pen(Color.FromArgb(36, 108, 196, 208), 1f);
        graphics.DrawRectangle(inner, Rectangle.Inflate(bounds, -4, -4));
    }

    private void DrawPanelTitle(Graphics graphics, Rectangle bounds, string title)
    {
        DrawHudText(graphics, title, 10.6f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), bounds.Left + 12, bounds.Top + 8);
        using var accent = new Pen(Color.FromArgb(132, 170, 146, 92), 1.4f);
        graphics.DrawLine(accent, bounds.Left + 12, bounds.Top + 26, bounds.Right - 12, bounds.Top + 26);
    }

}
