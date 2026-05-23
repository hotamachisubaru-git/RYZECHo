namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void DrawActors(Graphics graphics, PointF mousePosition)
    {
        if (_player.IsAlive)
        {
            DrawPlayerFov(graphics);
        }

        DrawSharedVisionFovs(graphics);

        DrawActor(graphics, _player, mousePosition);

        foreach (var ally in _allies)
        {
            DrawActor(graphics, ally, mousePosition);
        }

        foreach (var enemy in _enemies)
        {
            DrawEnemy(graphics, enemy);
        }
    }

    private void DrawActor(Graphics graphics, Actor actor, PointF mousePosition)
    {
        var center = actor.Position;
        var isPlayer = actor.Type == ActorType.Player;
        var color = actor.IsBoss ? Color.FromArgb(255, 245, 210, 110) : actor.Type == ActorType.Ally ? Color.FromArgb(255, 95, 225, 200) : Color.FromArgb(255, 75, 220, 245);

        if (isPlayer)
        {
            DrawBoardScanner(graphics, center, 68f, Color.FromArgb(96, 86, 229, 247));
        }

        using var shadow = new SolidBrush(Color.FromArgb(72, 0, 0, 0));
        graphics.FillEllipse(shadow, center.X - actor.Radius - 4f, center.Y - (actor.Radius * 0.25f), (actor.Radius * 2f) + 8f, actor.Radius + 12f);

        using var ringPen = new Pen(Color.FromArgb(isPlayer ? 240 : 180, color), isPlayer ? 2.4f : 1.8f);
        graphics.DrawEllipse(ringPen, center.X - actor.Radius - 8f, center.Y - 8f, (actor.Radius * 2f) + 16f, (actor.Radius * 1.25f) + 16f);

        using var fill = new SolidBrush(actor.IsAlive ? color : Color.FromArgb(80, 80, 80, 90));
        using var outline = new Pen(Color.FromArgb(220, 240, 250, 250), actor.IsBoss ? 2.8f : 2f);

        graphics.FillEllipse(fill, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);
        graphics.DrawEllipse(outline, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);

        var facing = new PointF(center.X + (MathF.Cos(actor.FacingAngle) * 22f), center.Y + (MathF.Sin(actor.FacingAngle) * 22f));
        using var directionPen = new Pen(Color.FromArgb(220, 240, 250, 250), 2f);
        graphics.DrawLine(directionPen, center, facing);

        using var textBrush = new SolidBrush(Color.FromArgb(230, 235, 245, 245));
        using var nameFont = new Font(UiFontFamily, isPlayer ? 11f : 9f, FontStyle.Bold);
        graphics.DrawString(actor.Name, nameFont, textBrush, center.X - 34f, center.Y - actor.Radius - 28f);

        var hpRatio = actor.Health / actor.MaxHealth;
        var shieldRatio = actor.MaxShield <= 0f ? 0f : actor.Shield / actor.MaxShield;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(220, 70, 220, 165));
        graphics.FillRectangle(hpBack, center.X - 28f, center.Y + actor.Radius + 6f, 56f, 5f);
        graphics.FillRectangle(hpFill, center.X - 28f, center.Y + actor.Radius + 6f, 56f * Math.Clamp(hpRatio, 0f, 1f), 5f);
        if (shieldRatio > 0f)
        {
            using var shieldFill = new SolidBrush(Color.FromArgb(220, 92, 168, 232));
            graphics.FillRectangle(shieldFill, center.X - 28f, center.Y + actor.Radius, 56f * Math.Clamp(shieldRatio, 0f, 1f), 4f);
        }
        DrawStatusEffects(graphics, actor, new PointF(center.X, center.Y - actor.Radius - 42f));

        if (isPlayer && _phase == GamePhase.Hunt && actor.IsAlive)
        {
            using var aimPen = new Pen(Color.FromArgb(180, 110, 235, 255), 1.6f);
            graphics.DrawLine(aimPen, center, mousePosition);
        }
    }

    private void DrawEnemy(Graphics graphics, Actor enemy)
    {
        if (!enemy.IsAlive)
        {
            return;
        }

        if (!PlayerCanSee(enemy))
        {
            return;
        }

        var sharedOnly = IsEnemySharedVisible(enemy) && !PlayerHasDirectSightTo(enemy.Position);

        var points = new[]
        {
            new PointF(enemy.Position.X, enemy.Position.Y - enemy.Radius - 2f),
            new PointF(enemy.Position.X + enemy.Radius + 2f, enemy.Position.Y),
            new PointF(enemy.Position.X, enemy.Position.Y + enemy.Radius + 2f),
            new PointF(enemy.Position.X - enemy.Radius - 2f, enemy.Position.Y),
        };

        using var shadow = new SolidBrush(Color.FromArgb(82, 0, 0, 0));
        graphics.FillEllipse(shadow, enemy.Position.X - enemy.Radius - 5f, enemy.Position.Y - (enemy.Radius * 0.1f), (enemy.Radius * 2f) + 10f, enemy.Radius + 12f);
        using var ringPen = new Pen(sharedOnly ? Color.FromArgb(210, 124, 214, 255) : Color.FromArgb(205, 236, 105, 90), 2f);
        using var glowPen = new Pen(sharedOnly ? Color.FromArgb(92, 164, 228, 255) : Color.FromArgb(92, 255, 164, 112), 1.2f);
        graphics.DrawEllipse(ringPen, enemy.Position.X - enemy.Radius - 8f, enemy.Position.Y - 6f, (enemy.Radius * 2f) + 16f, (enemy.Radius * 1.18f) + 14f);
        graphics.DrawEllipse(glowPen, enemy.Position.X - enemy.Radius - 18f, enemy.Position.Y - 16f, (enemy.Radius * 2f) + 36f, (enemy.Radius * 2f) + 24f);

        using var fill = new SolidBrush(sharedOnly ? Color.FromArgb(210, 124, 214, 255) : Color.FromArgb(235, 230, 95, 85));
        using var border = new Pen(Color.FromArgb(255, 255, 220, 210), 2f);
        graphics.FillPolygon(fill, points);
        graphics.DrawPolygon(border, points);

        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(220, 255, 140, 120));
        var ratio = enemy.Health / enemy.MaxHealth;
        var shieldRatio = enemy.MaxShield <= 0f ? 0f : enemy.Shield / enemy.MaxShield;
        if (shieldRatio > 0f)
        {
            using var shieldFill = new SolidBrush(Color.FromArgb(220, 92, 168, 232));
            graphics.FillRectangle(shieldFill, enemy.Position.X - 24f, enemy.Position.Y + enemy.Radius + 2f, 48f * Math.Clamp(shieldRatio, 0f, 1f), 4f);
        }
        graphics.FillRectangle(hpBack, enemy.Position.X - 24f, enemy.Position.Y + enemy.Radius + 8f, 48f, 5f);
        graphics.FillRectangle(hpFill, enemy.Position.X - 24f, enemy.Position.Y + enemy.Radius + 8f, 48f * ratio, 5f);
        DrawStatusEffects(graphics, enemy, new PointF(enemy.Position.X, enemy.Position.Y - enemy.Radius - 26f));
    }

    private void DrawPlayerFov(Graphics graphics)
    {
        DrawActorFovCone(
            graphics,
            _player,
            1f,
            Color.FromArgb(78, 248, 244, 214),
            Color.FromArgb(0, 120, 240, 255),
            Color.FromArgb(116, 244, 232, 172),
            1.1f);
    }

    private void DrawSharedVisionFovs(Graphics graphics)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return;
        }

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            DrawActorFovCone(
                graphics,
                ally,
                0.72f,
                Color.FromArgb(34, 124, 214, 255),
                Color.FromArgb(0, 124, 214, 255),
                Color.FromArgb(84, 124, 214, 255),
                0.9f);
        }
    }

    private void DrawActorFovCone(Graphics graphics, Actor actor, float rangeScale, Color centerColor, Color surroundColor, Color edgeColor, float edgeWidth)
    {
        var weapon = _weaponStats[actor.Weapon];
        var fovDegrees = GetFovDegrees(actor.Weapon);
        var range = weapon.VisionRange * rangeScale;
        var diameter = range * 2f;
        var startAngle = RadiansToDegrees(actor.FacingAngle) - (fovDegrees / 2f);

        using var path = new GraphicsPath();
        path.AddPie(actor.Position.X - range, actor.Position.Y - range, diameter, diameter, startAngle, fovDegrees);

        using var coneBrush = new PathGradientBrush(path)
        {
            CenterColor = centerColor,
            SurroundColors = [surroundColor],
        };
        using var edge = new Pen(edgeColor, edgeWidth);

        graphics.FillPath(coneBrush, path);
        graphics.DrawPath(edge, path);
    }

    private void DrawDirectionalCue(Graphics graphics, Ripple ripple, float progress, float soundAlpha)
    {
        DrawDirectionalCue(graphics, ripple, progress, soundAlpha, false);
    }

    private void DrawDirectionalCue(Graphics graphics, Ripple ripple, float progress, float soundAlpha, bool sharedOnly)
    {
        if (!_player.IsAlive)
        {
            return;
        }

        var direction = new PointF(ripple.Position.X - _player.Position.X, ripple.Position.Y - _player.Position.Y);
        var length = MathF.Max(1f, MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y)));
        direction = new PointF(direction.X / length, direction.Y / length);

        var tail = 34f + (8f * ripple.Strength);
        var head = 18f + (6f * ripple.Strength);
        var wing = ripple.Kind == RippleKind.Skill ? 11f : 8f;
        var anchor = new PointF(_player.Position.X + (direction.X * 54f), _player.Position.Y + (direction.Y * 54f));
        var side = new PointF(-direction.Y, direction.X);
        var fade = Math.Clamp(1f - progress, 0f, 1f);
        var alpha = (int)(225f * fade * soundAlpha);
        var sourceColor = sharedOnly ? Color.FromArgb(180, 124, 214, 255) : ripple.Color;
        var color = Color.FromArgb(Math.Clamp(alpha, 18, 225), sourceColor);
        var glow = Color.FromArgb(Math.Clamp(alpha / 3, 8, 72), sourceColor);

        var bodyStart = new PointF(anchor.X - (direction.X * tail * 0.35f), anchor.Y - (direction.Y * tail * 0.35f));
        var bodyEnd = new PointF(anchor.X + (direction.X * tail), anchor.Y + (direction.Y * tail));
        var headBase = new PointF(bodyEnd.X - (direction.X * head), bodyEnd.Y - (direction.Y * head));

        using var bodyPen = new Pen(color, ripple.Kind == RippleKind.Skill ? 3f : 3.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var glowPen = new Pen(glow, ripple.Kind == RippleKind.Skill ? 5.2f : 5.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        graphics.DrawLine(glowPen, bodyStart, bodyEnd);
        graphics.DrawLine(bodyPen, bodyStart, bodyEnd);

        if (ripple.Kind == RippleKind.Skill)
        {
            var zigA = new PointF(
                headBase.X + (side.X * wing * 0.4f),
                headBase.Y + (side.Y * wing * 0.4f));
            var zigB = new PointF(
                headBase.X - (side.X * wing * 0.9f),
                headBase.Y - (side.Y * wing * 0.9f));
            var tip = new PointF(bodyEnd.X + (direction.X * 5f), bodyEnd.Y + (direction.Y * 5f));
            graphics.DrawLines(glowPen, [zigA, zigB, tip]);
            graphics.DrawLines(bodyPen, [zigA, zigB, tip]);
            return;
        }

        var left = new PointF(headBase.X + (side.X * wing), headBase.Y + (side.Y * wing));
        var right = new PointF(headBase.X - (side.X * wing), headBase.Y - (side.Y * wing));
        using var headBrush = new SolidBrush(color);
        using var headGlow = new SolidBrush(glow);
        graphics.FillPolygon(headGlow, [bodyEnd, left, right]);
        graphics.FillPolygon(headBrush, [bodyEnd, left, right]);
    }

}
