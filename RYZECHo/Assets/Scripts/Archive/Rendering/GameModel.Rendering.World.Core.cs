#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawWorldEffects(Graphics graphics)
    {
        foreach (var effect in _worldEffects)
        {
            var progress = Math.Clamp(effect.Age / MathF.Max(0.001f, effect.Lifetime), 0f, 1f);
            var pulse = 0.75f + (0.25f * MathF.Sin((_uiPulseTime * 5.4f) + ((int)effect.Kind * 0.8f)));
            var radius = effect.Radius * (0.92f + (0.08f * pulse));
            var alpha = (int)(92f * (1f - (progress * 0.55f)));
            var fillColor = Color.FromArgb(alpha, effect.Color);
            var edgeColor = Color.FromArgb(Math.Clamp(alpha + 44, 42, 182), effect.Color);

            using var fill = new SolidBrush(fillColor);
            using var edge = new Pen(edgeColor, effect.Kind == WorldEffectKind.Lockdown ? 2.4f : 1.6f)
            {
                DashStyle = effect.Kind is WorldEffectKind.NanoSmoke or WorldEffectKind.SilenceZone ? DashStyle.Dash : DashStyle.Solid,
            };

            graphics.FillEllipse(fill, effect.Position.X - radius, effect.Position.Y - radius, radius * 2f, radius * 2f);
            graphics.DrawEllipse(edge, effect.Position.X - radius, effect.Position.Y - radius, radius * 2f, radius * 2f);

            var label = effect.Kind switch
            {
                WorldEffectKind.PoisonCloud => "毒霧",
                WorldEffectKind.DeadlyDome => "致死",
                WorldEffectKind.NanoSmoke => "煙幕",
                WorldEffectKind.SilenceZone => "無音",
                WorldEffectKind.HunterEye => "索敵",
                WorldEffectKind.Lockdown => "封鎖",
                _ => "障害",
            };

            DrawEffectTag(graphics, new PointF(effect.Position.X, effect.Position.Y - radius - 18f), label, effect.Color);
        }
    }

    private void DrawCore(Graphics graphics)
    {
        foreach (var site in GetBombSites())
        {
            var coreCenter = BombSitePosition(site.Id);
            var coreRect = new RectangleF(coreCenter.X - 22f, coreCenter.Y - 22f, 44f, 44f);
            var isActive = CurrentObjectiveSiteId() == site.Id;
            var isArmed = _bombPlanted && _armedBombSiteId == site.Id;
            var isPlanting = !_bombPlanted && _activePlanter is not null && TryGetBombSiteAt(_activePlanter.Position, out var planterSite, 10f) && planterSite.Id == site.Id;

            var siteGlow = isArmed
                ? Color.FromArgb(78, 255, 126, 96)
                : isActive
                    ? Color.FromArgb(64, 76, 228, 242)
                    : Color.FromArgb(28, 76, 228, 242);
            using var glow = new SolidBrush(siteGlow);
            graphics.FillEllipse(glow, coreCenter.X - 60f, coreCenter.Y - 60f, 120f, 120f);

            using var fill = new SolidBrush(isArmed ? Color.FromArgb(228, 218, 92, 78) : isActive ? Color.FromArgb(220, 48, 168, 198) : Color.FromArgb(164, 32, 118, 148));
            using var border = new Pen(Color.FromArgb(238, 214, 255, 255), isActive ? 2.5f : 1.8f);
            using var outerRing = new Pen(isArmed ? Color.FromArgb(104, 255, 174, 116) : Color.FromArgb(72, 118, 236, 246), 1.6f);
            graphics.FillEllipse(fill, coreRect);
            graphics.DrawEllipse(border, coreRect);
            graphics.DrawEllipse(outerRing, coreCenter.X - 40f, coreCenter.Y - 40f, 80f, 80f);

            var diamond = new[]
            {
                new PointF(coreCenter.X, coreCenter.Y - 12f),
                new PointF(coreCenter.X + 12f, coreCenter.Y),
                new PointF(coreCenter.X, coreCenter.Y + 12f),
                new PointF(coreCenter.X - 12f, coreCenter.Y),
            };
            using var diamondPen = new Pen(Color.FromArgb(248, 232, 255, 255), 1.6f);
            graphics.DrawPolygon(diamondPen, diamond);

            var ratio = isArmed
                ? Math.Clamp(_roundTimer / BombFuseSeconds, 0f, 1f)
                : isPlanting
                    ? Math.Clamp(_bombPlantProgress / BombPlantSeconds, 0f, 1f)
                    : 0f;
            using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            using var hpFill = new SolidBrush(isArmed ? Color.FromArgb(232, 255, 134, 112) : Color.FromArgb(225, 70, 220, 210));
            var hpRect = new RectangleF(coreCenter.X - 40f, coreCenter.Y + 28f, 80f, 7f);
            graphics.FillRectangle(hpBack, hpRect);
            if (ratio > 0f)
            {
                graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
            }

            if (isArmed)
            {
                var secondaryRatio = Math.Clamp(_bombDefuseProgress / BombDefuseSeconds, 0f, 1f);
                if (secondaryRatio > 0f)
                {
                    using var defuseFill = new SolidBrush(Color.FromArgb(228, 108, 228, 210));
                    graphics.FillRectangle(defuseFill, hpRect.Left, hpRect.Top - 10f, hpRect.Width * secondaryRatio, 5f);
                }
            }

            DrawHudText(graphics, site.Label, 8.4f, FontStyle.Bold, Color.FromArgb(236, 238, 244, 248), coreCenter.X - 7f, coreCenter.Y - 42f);
            DrawHudText(graphics, isArmed ? "ARMED" : isPlanting ? "PLANT" : "SITE", 6.8f, FontStyle.Bold, Color.FromArgb(236, 238, 244, 248), coreCenter.X - 18f, coreCenter.Y - 30f);
        }
    }
}
#endif
