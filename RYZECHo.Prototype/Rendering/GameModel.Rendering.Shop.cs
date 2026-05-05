namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void DrawChoiceCard(Graphics graphics, Rectangle bounds, string keyLabel, string title, string subtitle, bool selected, Color accent)
    {
        using var fill = new SolidBrush(selected ? Color.FromArgb(120, accent) : Color.FromArgb(70, 16, 24, 30));
        using var border = new Pen(selected ? Color.FromArgb(240, accent) : Color.FromArgb(100, 76, 110, 124), selected ? 2.2f : 1.4f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);

        DrawHudText(graphics, keyLabel, 9f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), bounds.Left + 10, bounds.Top + 8);
        DrawHudText(graphics, title, 10.2f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), bounds.Left + 10, bounds.Top + 30);
        DrawHudText(graphics, subtitle, 8.8f, FontStyle.Regular, Color.FromArgb(225, 204, 218, 226), bounds.Left + 10, bounds.Top + 56);
    }

    private void DrawBetShopList(Graphics graphics, Rectangle bounds)
    {
        var catalog = _selectedLoadoutFocus == LoadoutFocus.Primary ? PrimaryWeaponSelectionOrder() : SidearmSelectionOrder();
        var selected = SelectedLoadoutWeapon();
        var selectedIndex = Array.IndexOf(catalog, selected);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        DrawHudText(graphics, _selectedLoadoutFocus == LoadoutFocus.Primary ? "メインショップ" : "サブショップ", 8.2f, FontStyle.Bold, Color.FromArgb(236, 224, 232, 240), bounds.Left, bounds.Top);
        var cardHeight = 42;
        var visible = Math.Clamp((bounds.Height - 18) / (cardHeight + 8), 1, Math.Min(3, catalog.Length));
        var cardWidth = bounds.Width;
        var start = Math.Clamp(selectedIndex - 1, 0, Math.Max(0, catalog.Length - visible));
        for (var offset = 0; offset < visible; offset++)
        {
            var weaponType = catalog[start + offset];
            var rect = new Rectangle(bounds.Left, bounds.Top + 18 + (offset * (cardHeight + 8)), cardWidth, cardHeight);
            DrawChoiceCard(
                graphics,
                rect,
                start + offset == selectedIndex ? "SEL" : $"{start + offset + 1}",
                _weaponStats[weaponType].Label,
                $"{_weaponStats[weaponType].Cost}c / {_weaponStats[weaponType].MagazineAmmo}+{_weaponStats[weaponType].ReserveAmmo} / {_weaponStats[weaponType].Category}",
                weaponType == selected,
                WeaponAccent(weaponType));
        }
    }

    private void DrawWeaponChoice(Graphics graphics, Rectangle bounds, string keyLabel, WeaponType weaponType, bool selected)
    {
        var weapon = _weaponStats[weaponType];
        DrawChoiceCard(graphics, bounds, keyLabel, weapon.Label, $"{weapon.Cost}c / {weapon.MagazineAmmo}+{weapon.ReserveAmmo} / {weapon.VisionClass}視界", selected, WeaponAccent(weaponType));
    }

    private Color WeaponAccent(WeaponType weaponType)
    {
        return _weaponStats[weaponType].Category switch
        {
            "近距離特化" => Color.FromArgb(255, 255, 196, 82),
            "遠距離特化" => Color.FromArgb(255, 245, 170, 120),
            "サブウェポン" => Color.FromArgb(255, 170, 214, 255),
            _ => Color.FromArgb(255, 92, 220, 235),
        };
    }

    private string SidearmLoadoutLabel(WeaponType weaponType)
    {
        return _weaponStats[weaponType].Category == "遠距離特化" ? "BLD" : "PNS";
    }

    private (Color Top, Color Side, Color Outline, Color Fill) StructureDoorPalette()
    {
        return SelectedStructureSkinName() switch
        {
            "カーボンゲート" => (
                Color.FromArgb(138, 102, 112, 128),
                Color.FromArgb(54, 18, 24, 32),
                Color.FromArgb(232, 214, 224, 236),
                Color.FromArgb(220, 168, 178, 196)),
            "サンドパルス" => (
                Color.FromArgb(138, 184, 138, 98),
                Color.FromArgb(54, 58, 34, 22),
                Color.FromArgb(232, 250, 224, 172),
                Color.FromArgb(220, 244, 188, 126)),
            _ => (
                Color.FromArgb(128, 90, 162, 204),
                Color.FromArgb(48, 14, 44, 70),
                Color.FromArgb(232, 156, 238, 248),
                Color.FromArgb(220, 88, 228, 220)),
        };
    }

    private (Color HoneyFill, Color HoneyOutline, Color NestFill, Color NestOutline) StructureTrapPalette()
    {
        return SelectedStructureSkinName() switch
        {
            "カーボンゲート" => (
                Color.FromArgb(182, 142, 168, 188),
                Color.FromArgb(255, 214, 228, 240),
                Color.FromArgb(128, 126, 164, 188),
                Color.FromArgb(232, 210, 228, 244)),
            "サンドパルス" => (
                Color.FromArgb(182, 238, 180, 106),
                Color.FromArgb(255, 252, 226, 178),
                Color.FromArgb(126, 180, 226, 132),
                Color.FromArgb(232, 238, 246, 174)),
            _ => (
                Color.FromArgb(178, 238, 168, 62),
                Color.FromArgb(255, 255, 230, 164),
                Color.FromArgb(116, 92, 214, 128),
                Color.FromArgb(225, 214, 255, 180)),
        };
    }

    private Color AdThemeAccent()
    {
        return SelectedAdThemeName() switch
        {
            "VERTEX CUP" => Color.FromArgb(255, 236, 134, 96),
            "SUNSET GRID" => Color.FromArgb(255, 248, 184, 118),
            _ => Color.FromArgb(255, 98, 228, 242),
        };
    }

    private void DrawSoundEdgeIndicators(Graphics graphics)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return;
        }

        var indicators = _ripples
            .Where(ripple => TeamCanPerceive(ripple.Position, ripple.Strength))
            .Where(ripple => !PlayerHasDirectSightTo(ripple.Position))
            .OrderByDescending(ripple => ripple.Strength)
            .Take(3)
            .ToArray();

        if (indicators.Length == 0)
        {
            return;
        }

        var cameraBounds = MainPlayCameraBounds;
        var center = new PointF(
            cameraBounds.Left + (cameraBounds.Width * HuntCameraTargetX),
            cameraBounds.Top + (cameraBounds.Height * HuntCameraTargetY));
        foreach (var ripple in indicators)
        {
            var direction = new PointF(ripple.Position.X - _player.Position.X, ripple.Position.Y - _player.Position.Y);
            var length = MathF.Max(1f, MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y)));
            direction = new PointF(direction.X / length, direction.Y / length);
            var anchor = new PointF(center.X + (direction.X * 220f), center.Y + (direction.Y * 140f));
            var side = new PointF(-direction.Y, direction.X);
            var color = TeamCanPerceive(ripple.Position, ripple.Strength) && !PlayerCanPerceive(ripple.Position, ripple.Strength)
                ? Color.FromArgb(210, 124, 214, 255)
                : Color.FromArgb(220, ripple.Color);

            var tip = new PointF(anchor.X + (direction.X * 14f), anchor.Y + (direction.Y * 14f));
            var left = new PointF(anchor.X - (direction.X * 8f) + (side.X * 8f), anchor.Y - (direction.Y * 8f) + (side.Y * 8f));
            var right = new PointF(anchor.X - (direction.X * 8f) - (side.X * 8f), anchor.Y - (direction.Y * 8f) - (side.Y * 8f));
            using var brush = new SolidBrush(color);
            graphics.FillPolygon(brush, [tip, left, right]);
        }
    }

    private void DrawMiniMapActor(Graphics graphics, Rectangle inner, RectangleF viewRect, float scaleX, float scaleY, Actor actor, Color color)
    {
        if (!actor.IsAlive)
        {
            return;
        }

        if (!viewRect.Contains(actor.Position))
        {
            return;
        }

        using var brush = new SolidBrush(color);
        var point = new PointF(inner.Left + ((actor.Position.X - viewRect.Left) * scaleX), inner.Top + ((actor.Position.Y - viewRect.Top) * scaleY));
        var size = actor.IsBoss ? 8f : 6f;
        graphics.FillEllipse(brush, point.X - (size / 2f), point.Y - (size / 2f), size, size);
    }

}
