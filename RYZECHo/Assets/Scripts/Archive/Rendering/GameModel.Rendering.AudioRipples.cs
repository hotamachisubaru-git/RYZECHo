#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed partial class GameModel
{
    private void DrawRipples(Graphics graphics)
    {
        foreach (var ripple in _ripples)
        {
            if (_phase == GamePhase.Hunt && !_player.IsAlive)
            {
                continue;
            }

            if (_phase == GamePhase.Hunt && !TeamCanPerceive(ripple.Position, ripple.Strength))
            {
                continue;
            }

            var progress = ripple.Age / ripple.Lifetime;
            var occlusion = GetAudioOcclusionProfile(ripple.Position);
            var sharedOnly = _phase == GamePhase.Hunt && !PlayerCanPerceive(ripple.Position, ripple.Strength);

            if (ripple.Kind is RippleKind.Footstep or RippleKind.Breathing)
            {
                DrawRippleRing(graphics, ripple, progress, occlusion, sharedOnly);
                continue;
            }

            if (_phase == GamePhase.Hunt && PlayerHasDirectSightTo(ripple.Position))
            {
                continue;
            }

            DrawDirectionalCue(graphics, ripple, progress, occlusion, sharedOnly);
        }
    }

    private void DrawRippleRing(
        Graphics graphics,
        Ripple ripple,
        float progress,
        AudioOcclusionProfile occlusion,
        bool sharedOnly)
    {
        var visual = AudioRippleVisualRules.CreateRingVisual(ripple, progress, occlusion, sharedOnly);
        using var pen = new Pen(visual.RingColor, visual.StrokeWidth);
        using var halo = new Pen(visual.HaloColor, visual.HaloStrokeWidth)
        {
            DashStyle = occlusion.OccludingCells > 0 ? DashStyle.Dash : DashStyle.Solid,
        };

        graphics.DrawEllipse(pen, ripple.Position.X - visual.Radius, ripple.Position.Y - visual.Radius, visual.Radius * 2f, visual.Radius * 2f);
        graphics.DrawEllipse(halo, ripple.Position.X - visual.HaloRadius, ripple.Position.Y - visual.HaloRadius, visual.HaloRadius * 2f, visual.HaloRadius * 2f);
    }

    private void DrawDirectionalCue(
        Graphics graphics,
        Ripple ripple,
        float progress,
        AudioOcclusionProfile occlusion,
        bool sharedOnly)
    {
        if (!_player.IsAlive)
        {
            return;
        }

        var visual = AudioRippleVisualRules.CreateDirectionalCue(_player.Position, ripple, progress, occlusion, sharedOnly);
        using var bodyPen = new Pen(visual.BodyColor, visual.BodyWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var glowPen = new Pen(visual.GlowColor, visual.GlowWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
            DashStyle = occlusion.OccludingCells > 0 ? DashStyle.Dash : DashStyle.Solid,
        };

        graphics.DrawLine(glowPen, visual.BodyStart, visual.BodyEnd);
        graphics.DrawLine(bodyPen, visual.BodyStart, visual.BodyEnd);

        if (visual.UsesZigZag)
        {
            graphics.DrawLines(glowPen, [visual.ZigA, visual.ZigB, visual.Tip]);
            graphics.DrawLines(bodyPen, [visual.ZigA, visual.ZigB, visual.Tip]);
            return;
        }

        using var headBrush = new SolidBrush(visual.BodyColor);
        using var headGlow = new SolidBrush(visual.GlowColor);
        graphics.FillPolygon(headGlow, [visual.BodyEnd, visual.LeftWing, visual.RightWing]);
        graphics.FillPolygon(headBrush, [visual.BodyEnd, visual.LeftWing, visual.RightWing]);
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
            var sharedOnly = !PlayerCanPerceive(ripple.Position, ripple.Strength);
            var visual = AudioRippleVisualRules.CreateEdgeArrow(_player.Position, center, ripple, sharedOnly);
            using var brush = new SolidBrush(visual.Color);
            graphics.FillPolygon(brush, [visual.Tip, visual.Left, visual.Right]);
        }
    }
}


#endif
