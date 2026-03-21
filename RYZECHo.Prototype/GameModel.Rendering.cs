using System.Drawing.Drawing2D;

namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    public void Render(Graphics graphics, Rectangle clientBounds, Point mousePosition)
    {
        _layoutSize = clientBounds.Size;

        using var background = new LinearGradientBrush(clientBounds, Color.FromArgb(7, 14, 22), Color.FromArgb(3, 8, 14), 90f);
        graphics.FillRectangle(background, clientBounds);
        using var vignette = new LinearGradientBrush(clientBounds, Color.FromArgb(0, 86, 229, 247), Color.FromArgb(26, 20, 54, 84), 22f);
        graphics.FillRectangle(vignette, clientBounds);

        DrawWorldDropShadow(graphics);

        var worldMousePosition = ScreenToWorldPoint(mousePosition);
        var graphicsState = graphics.Save();
        using (var worldTransform = CreateActiveWorldMatrix())
        {
            graphics.MultiplyTransform(worldTransform);
            DrawWorldPanel(graphics);
            DrawStructures(graphics);
            DrawCore(graphics);
            DrawRipples(graphics);
            DrawActors(graphics, worldMousePosition);
        }

        graphics.Restore(graphicsState);
        DrawHud(graphics);

        if (_showBriefing)
        {
            DrawBriefingOverlay(graphics);
        }
    }

    private void DrawWorldDropShadow(Graphics graphics)
    {
        var corners = GetProjectedWorldCorners();
        var shadow = corners.Select(point => new PointF(point.X + 18f, point.Y + 20f)).ToArray();
        using var shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
        graphics.FillPolygon(shadowBrush, shadow);
    }

    private void DrawWorldPanel(Graphics graphics)
    {
        using var worldBrush = new LinearGradientBrush(WorldBounds, Color.FromArgb(10, 22, 32), Color.FromArgb(4, 11, 18), 90f);
        graphics.FillRectangle(worldBrush, WorldBounds);

        DrawTacticalSurface(graphics);
        DrawSignalLane(graphics, new[]
        {
            new PointF(WorldBounds.Left + 46f, WorldBounds.Top + 392f),
            new PointF(WorldBounds.Left + 246f, WorldBounds.Top + 318f),
            new PointF(WorldBounds.Left + 536f, WorldBounds.Top + 276f),
            new PointF(WorldBounds.Right - 56f, WorldBounds.Top + 308f),
            new PointF(WorldBounds.Right - 84f, WorldBounds.Top + 382f),
            new PointF(WorldBounds.Left + 572f, WorldBounds.Top + 344f),
            new PointF(WorldBounds.Left + 258f, WorldBounds.Top + 382f),
            new PointF(WorldBounds.Left + 30f, WorldBounds.Top + 460f),
        }, Color.FromArgb(54, 234, 220, 182), Color.FromArgb(122, 246, 236, 214));
        DrawSignalLane(graphics, new[]
        {
            new PointF(WorldBounds.Left + 118f, WorldBounds.Top + 562f),
            new PointF(WorldBounds.Left + 328f, WorldBounds.Top + 440f),
            new PointF(WorldBounds.Left + 628f, WorldBounds.Top + 418f),
            new PointF(WorldBounds.Right - 112f, WorldBounds.Top + 468f),
            new PointF(WorldBounds.Right - 136f, WorldBounds.Top + 516f),
            new PointF(WorldBounds.Left + 620f, WorldBounds.Top + 472f),
            new PointF(WorldBounds.Left + 346f, WorldBounds.Top + 490f),
            new PointF(WorldBounds.Left + 140f, WorldBounds.Top + 610f),
        }, Color.FromArgb(36, 194, 230, 214), Color.FromArgb(98, 224, 240, 236));

        DrawCornerRelay(graphics, new PointF(WorldBounds.Left + 126f, WorldBounds.Top + 126f), 60f, Color.FromArgb(84, 104, 216, 104));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Left + 142f, WorldBounds.Bottom - 108f), 72f, Color.FromArgb(84, 94, 214, 122));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Right - 126f, WorldBounds.Top + 118f), 66f, Color.FromArgb(84, 104, 216, 104));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Right - 154f, WorldBounds.Bottom - 96f), 78f, Color.FromArgb(84, 94, 214, 122));

        DrawBoardScanner(graphics, new PointF(WorldBounds.Left + (WorldBounds.Width * 0.52f), WorldBounds.Top + (WorldBounds.Height * 0.52f)), 186f, Color.FromArgb(88, 86, 229, 247));
        DrawArenaBillboards(graphics);

        foreach (var cell in _permanentWalls)
        {
            var rectangle = CellRectangle(cell);
            var tile = Rectangle.Inflate(rectangle, -2, -2);
            if (IsPerimeterCell(cell))
            {
                DrawBoardTile(graphics, tile, Color.FromArgb(82, 70, 126, 150), Color.FromArgb(176, 118, 232, 246), true);
            }
            else
            {
                DrawRaisedBlock(graphics, tile, Color.FromArgb(90, 82, 132, 160), Color.FromArgb(42, 16, 36, 52), Color.FromArgb(210, 138, 228, 246), 14f);
            }
        }

        if (_phase == GamePhase.Construct)
        {
            foreach (var slot in _buildSlots)
            {
                DrawBuildSlotMarker(graphics, slot);
            }
        }

        using var borderPen = new Pen(Color.FromArgb(110, 116, 220, 236), 2.2f);
        graphics.DrawRectangle(borderPen, WorldBounds);
        using var innerPen = new Pen(Color.FromArgb(70, 84, 152, 172), 1f);
        graphics.DrawRectangle(innerPen, Rectangle.Inflate(WorldBounds, -10, -10));
    }

    private void DrawTacticalSurface(Graphics graphics)
    {
        var inner = Rectangle.Inflate(WorldBounds, -12, -12);
        using var boardBrush = new LinearGradientBrush(inner, Color.FromArgb(18, 32, 42), Color.FromArgb(6, 16, 22), 90f);
        graphics.FillRectangle(boardBrush, inner);

        using var sweepBrush = new LinearGradientBrush(inner, Color.FromArgb(0, 86, 229, 247), Color.FromArgb(36, 86, 229, 247), 24f);
        graphics.FillRectangle(sweepBrush, inner);

        using var gridMinor = new Pen(Color.FromArgb(24, 86, 229, 247), 1f);
        using var gridMajor = new Pen(Color.FromArgb(62, 86, 229, 247), 1.6f);
        for (var x = 0; x <= GridColumns; x++)
        {
            var xPos = WorldBounds.Left + (x * CellSize);
            graphics.DrawLine(x % 3 == 0 ? gridMajor : gridMinor, xPos, WorldBounds.Top, xPos, WorldBounds.Bottom);
        }

        for (var y = 0; y <= GridRows; y++)
        {
            var yPos = WorldBounds.Top + (y * CellSize);
            graphics.DrawLine(y % 3 == 0 ? gridMajor : gridMinor, WorldBounds.Left, yPos, WorldBounds.Right, yPos);
        }

        using var pathPen = new Pen(Color.FromArgb(40, 200, 245, 255), 2f);
        graphics.DrawBezier(pathPen,
            new PointF(WorldBounds.Left + 118f, WorldBounds.Top + 188f),
            new PointF(WorldBounds.Left + 312f, WorldBounds.Top + 234f),
            new PointF(WorldBounds.Left + 612f, WorldBounds.Top + 204f),
            new PointF(WorldBounds.Right - 142f, WorldBounds.Top + 286f));
        graphics.DrawBezier(pathPen,
            new PointF(WorldBounds.Left + 138f, WorldBounds.Bottom - 118f),
            new PointF(WorldBounds.Left + 366f, WorldBounds.Top + 492f),
            new PointF(WorldBounds.Left + 654f, WorldBounds.Top + 470f),
            new PointF(WorldBounds.Right - 166f, WorldBounds.Bottom - 104f));
    }

    private void DrawSignalLane(Graphics graphics, PointF[] points, Color fillColor, Color outlineColor)
    {
        using var lanePath = new GraphicsPath();
        lanePath.AddPolygon(points);

        using var laneBrush = new SolidBrush(fillColor);
        using var edgePen = new Pen(outlineColor, 2.4f)
        {
            LineJoin = LineJoin.Round,
        };

        graphics.FillPath(laneBrush, lanePath);
        graphics.DrawPath(edgePen, lanePath);
    }

    private void DrawCornerRelay(Graphics graphics, PointF center, float radius, Color accent)
    {
        using var shadow = new SolidBrush(Color.FromArgb(38, 0, 0, 0));
        graphics.FillEllipse(shadow, center.X - radius + 10f, center.Y - (radius * 0.45f) + 10f, radius * 2f, radius * 0.9f);

        using var glow = new SolidBrush(accent);
        using var rim = new Pen(Color.FromArgb(148, 146, 244, 146), 2f);
        graphics.FillEllipse(glow, center.X - radius, center.Y - (radius * 0.56f), radius * 2f, radius * 1.12f);
        graphics.DrawEllipse(rim, center.X - radius, center.Y - (radius * 0.56f), radius * 2f, radius * 1.12f);
    }

    private void DrawBoardScanner(Graphics graphics, PointF center, float radius, Color accent)
    {
        using var ringPen = new Pen(accent, 2f);
        using var outerPen = new Pen(Color.FromArgb(48, accent), 1.2f);
        using var crossPen = new Pen(Color.FromArgb(120, accent), 1.2f);
        graphics.DrawEllipse(outerPen, center.X - radius - 42f, center.Y - radius - 42f, (radius + 42f) * 2f, (radius + 42f) * 2f);
        graphics.DrawEllipse(ringPen, center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
        graphics.DrawEllipse(outerPen, center.X - (radius * 0.48f), center.Y - (radius * 0.48f), radius * 0.96f, radius * 0.96f);
        graphics.DrawLine(crossPen, center.X - radius - 18f, center.Y, center.X + radius + 18f, center.Y);
        graphics.DrawLine(crossPen, center.X, center.Y - radius - 18f, center.X, center.Y + radius + 18f);
    }

    private void DrawArenaBillboards(Graphics graphics)
    {
        var accent = AdThemeAccent();
        var labels = SelectedAdThemeName() switch
        {
            "VERTEX CUP" => new[] { "VERTEX CUP", "TACTICAL CIRCUIT", "BUILD. BREAK. WIN." },
            "SUNSET GRID" => new[] { "SUNSET GRID", "ARENA PASS", "STYLE ONLY" },
            _ => new[] { "NEO CORE", "SIGNAL MARKET", "LIVE WALL SIGNAGE" },
        };

        var strips = new[]
        {
            new Rectangle(WorldBounds.Left + 72, WorldBounds.Top + 10, 152, 20),
            new Rectangle(WorldBounds.Right - 228, WorldBounds.Top + 10, 156, 20),
            new Rectangle(WorldBounds.Right - 30, WorldBounds.Top + 110, 20, 136),
        };

        for (var index = 0; index < strips.Length; index++)
        {
            var strip = strips[index];
            using var fill = new LinearGradientBrush(strip, Color.FromArgb(112, 12, 22, 32), Color.FromArgb(76, 6, 10, 16), 90f);
            using var border = new Pen(Color.FromArgb(132, accent), 1.2f);
            using var inner = new Pen(Color.FromArgb(40, accent), 1f);
            graphics.FillRectangle(fill, strip);
            graphics.DrawRectangle(border, strip);
            graphics.DrawRectangle(inner, Rectangle.Inflate(strip, -3, -3));

            if (index < 2)
            {
                DrawHudText(graphics, labels[index], 6.8f, FontStyle.Bold, Color.FromArgb(174, 240, 242, 246), strip.Left + 8, strip.Top + 4);
            }
            else
            {
                var state = graphics.Save();
                graphics.TranslateTransform(strip.Left + 6, strip.Bottom - 8);
                graphics.RotateTransform(-90f);
                DrawHudText(graphics, labels[index], 6.8f, FontStyle.Bold, Color.FromArgb(162, 240, 242, 246), 0f, 0f);
                graphics.Restore(state);
            }
        }
    }

    private void DrawBoardTile(Graphics graphics, Rectangle rectangle, Color fillColor, Color outlineColor, bool diagonalCut)
    {
        using var fill = new LinearGradientBrush(rectangle, fillColor, Color.FromArgb(34, 18, 44, 62), 90f);
        using var border = new Pen(outlineColor, 1.6f);
        using var detail = new Pen(Color.FromArgb(122, outlineColor), 1f);
        graphics.FillRectangle(fill, rectangle);
        graphics.DrawRectangle(border, rectangle);
        if (diagonalCut)
        {
            graphics.DrawLine(detail, rectangle.Left + 8, rectangle.Bottom - 8, rectangle.Right - 8, rectangle.Top + 8);
        }
    }

    private void DrawRaisedBlock(Graphics graphics, Rectangle rectangle, Color topColor, Color sideColor, Color outlineColor, float height)
    {
        var sideFace = new[]
        {
            new PointF(rectangle.Left, rectangle.Bottom),
            new PointF(rectangle.Right, rectangle.Bottom),
            new PointF(rectangle.Right + 8f, rectangle.Bottom + height),
            new PointF(rectangle.Left + 8f, rectangle.Bottom + height),
        };

        using var sideBrush = new SolidBrush(sideColor);
        using var topBrush = new SolidBrush(topColor);
        using var outlinePen = new Pen(outlineColor, 1.8f);
        graphics.FillPolygon(sideBrush, sideFace);
        graphics.FillRectangle(topBrush, rectangle);
        graphics.DrawPolygon(outlinePen, sideFace);
        graphics.DrawRectangle(outlinePen, rectangle);
        using var detailPen = new Pen(Color.FromArgb(120, 214, 240, 248), 1f);
        graphics.DrawLine(detailPen, rectangle.Left + 8f, rectangle.Bottom - 8f, rectangle.Right - 8f, rectangle.Top + 8f);
    }

    private void DrawBuildSlotMarker(Graphics graphics, Point slot)
    {
        var center = CellCenter(slot);
        if (IsNoBuildCell(slot))
        {
            using var lockedPen = new Pen(Color.FromArgb(184, 255, 118, 102), 2f)
            {
                DashStyle = DashStyle.Dash,
            };
            using var lockedHaloPen = new Pen(Color.FromArgb(72, 255, 118, 102), 1.2f);
            graphics.DrawEllipse(lockedPen, center.X - 18f, center.Y - 18f, 36f, 36f);
            graphics.DrawEllipse(lockedHaloPen, center.X - 28f, center.Y - 28f, 56f, 56f);
            graphics.DrawLine(lockedPen, center.X - 10f, center.Y - 10f, center.X + 10f, center.Y + 10f);
            graphics.DrawLine(lockedPen, center.X + 10f, center.Y - 10f, center.X - 10f, center.Y + 10f);
            return;
        }

        var accent = WeaponAccent(_selectedWeapon);
        using var outerPen = new Pen(Color.FromArgb(148, 84, 214, 228), 2f)
        {
            DashStyle = DashStyle.Dash,
        };
        using var innerBrush = new SolidBrush(Color.FromArgb(74, accent));
        using var haloPen = new Pen(Color.FromArgb(52, 116, 236, 248), 1.2f);
        graphics.FillEllipse(innerBrush, center.X - 10f, center.Y - 10f, 20f, 20f);
        graphics.DrawEllipse(outerPen, center.X - 18f, center.Y - 18f, 36f, 36f);
        graphics.DrawEllipse(haloPen, center.X - 30f, center.Y - 30f, 60f, 60f);
    }

    private void DrawStructures(Graphics graphics)
    {
        var doorPalette = StructureDoorPalette();
        var trapPalette = StructureTrapPalette();
        foreach (var structure in _structures)
        {
            var rectangle = Rectangle.Inflate(CellRectangle(structure.Cell), -6, -6);

            switch (structure.Kind)
            {
                case StructureKind.BlastDoor:
                    DrawRaisedBlock(graphics, rectangle, doorPalette.Top, doorPalette.Side, doorPalette.Outline, 18f);

                    var ratio = structure.Health / structure.MaxHealth;
                    using (var hpBack = new SolidBrush(Color.FromArgb(36, 0, 0, 0)))
                    using (var hpFill = new SolidBrush(doorPalette.Fill))
                    {
                        var hpRect = new RectangleF(rectangle.Left, rectangle.Bottom + 3f, rectangle.Width, 5f);
                        graphics.FillRectangle(hpBack, hpRect);
                        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
                    }
                    break;
                case StructureKind.HoneyTrap:
                    using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 6, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(trapPalette.HoneyFill))
                    using (var pen = new Pen(trapPalette.HoneyOutline, 2.2f))
                    using (var ring = new Pen(Color.FromArgb(88, trapPalette.HoneyOutline), 1.4f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                        graphics.DrawEllipse(ring, rectangle.Left - 12, rectangle.Top - 12, rectangle.Width + 24, rectangle.Height + 24);
                    }
                    break;
                case StructureKind.StaticNest:
                    using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 6, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(trapPalette.NestFill))
                    using (var pen = new Pen(trapPalette.NestOutline, 2f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                    }

                    using (var auraPen = new Pen(Color.FromArgb(92, trapPalette.NestOutline), 1.5f))
                    {
                        graphics.DrawEllipse(auraPen, rectangle.Left - 20, rectangle.Top - 20, rectangle.Width + 40, rectangle.Height + 40);
                        graphics.DrawEllipse(auraPen, rectangle.Left - 34, rectangle.Top - 34, rectangle.Width + 68, rectangle.Height + 68);
                    }
                    break;
            }
        }
    }

    private void DrawCore(Graphics graphics)
    {
        var coreCenter = BombSitePosition();
        var coreRect = new RectangleF(coreCenter.X - 24f, coreCenter.Y - 24f, 48f, 48f);

        var siteGlow = _bombPlanted ? Color.FromArgb(72, 255, 126, 96) : Color.FromArgb(56, 76, 228, 242);
        using var glow = new SolidBrush(siteGlow);
        graphics.FillEllipse(glow, coreCenter.X - 64f, coreCenter.Y - 64f, 128f, 128f);

        using var fill = new SolidBrush(_bombPlanted ? Color.FromArgb(228, 218, 92, 78) : Color.FromArgb(220, 48, 168, 198));
        using var border = new Pen(Color.FromArgb(238, 214, 255, 255), 2.5f);
        using var outerRing = new Pen(_bombPlanted ? Color.FromArgb(104, 255, 174, 116) : Color.FromArgb(88, 118, 236, 246), 1.8f);
        graphics.FillEllipse(fill, coreRect);
        graphics.DrawEllipse(border, coreRect);
        graphics.DrawEllipse(outerRing, coreCenter.X - 42f, coreCenter.Y - 42f, 84f, 84f);
        graphics.DrawEllipse(outerRing, coreCenter.X - 78f, coreCenter.Y - 78f, 156f, 156f);

        var diamond = new[]
        {
            new PointF(coreCenter.X, coreCenter.Y - 14f),
            new PointF(coreCenter.X + 14f, coreCenter.Y),
            new PointF(coreCenter.X, coreCenter.Y + 14f),
            new PointF(coreCenter.X - 14f, coreCenter.Y),
        };
        using var diamondPen = new Pen(Color.FromArgb(248, 232, 255, 255), 1.8f);
        graphics.DrawPolygon(diamondPen, diamond);

        var ratio = _bombPlanted
            ? Math.Clamp(_roundTimer / BombFuseSeconds, 0f, 1f)
            : Math.Clamp(_bombPlantProgress / BombPlantSeconds, 0f, 1f);
        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(_bombPlanted ? Color.FromArgb(232, 255, 134, 112) : Color.FromArgb(225, 70, 220, 210));
        var hpRect = new RectangleF(coreCenter.X - 50f, coreCenter.Y + 30f, 100f, 8f);
        graphics.FillRectangle(hpBack, hpRect);
        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);

        var secondaryRatio = _bombPlanted ? Math.Clamp(_bombDefuseProgress / BombDefuseSeconds, 0f, 1f) : 0f;
        if (secondaryRatio > 0f)
        {
            using var defuseFill = new SolidBrush(Color.FromArgb(228, 108, 228, 210));
            graphics.FillRectangle(defuseFill, hpRect.Left, hpRect.Top - 11f, hpRect.Width * secondaryRatio, 6f);
        }

        DrawHudText(
            graphics,
            _bombPlanted ? "ARMED" : _bombPlantProgress > 0f ? "PLANT" : "SITE",
            7.6f,
            FontStyle.Bold,
            Color.FromArgb(236, 238, 244, 248),
            coreCenter.X - 18f,
            coreCenter.Y - 44f);
    }

    private void DrawRipples(Graphics graphics)
    {
        foreach (var ripple in _ripples)
        {
            if (_phase == GamePhase.Hunt && !_player.IsAlive)
            {
                continue;
            }

            if (_phase == GamePhase.Hunt && !PlayerCanPerceive(ripple.Position, ripple.Strength))
            {
                continue;
            }

            var progress = ripple.Age / ripple.Lifetime;
            var soundAlpha = GetSoundAlphaMultiplier(ripple.Position);

            if (ripple.Kind == RippleKind.Footstep)
            {
                var radius = 16f + (progress * 84f * ripple.Strength);
                var alpha = (int)(150f * (1f - progress) * soundAlpha);
                var color = Color.FromArgb(Math.Clamp(alpha, 12, 165), ripple.Color);

                using var pen = new Pen(color, 2f);
                using var halo = new Pen(Color.FromArgb(Math.Clamp(alpha / 2, 8, 80), ripple.Color), 1f);
                graphics.DrawEllipse(pen, ripple.Position.X - radius, ripple.Position.Y - radius, radius * 2f, radius * 2f);
                graphics.DrawEllipse(halo, ripple.Position.X - radius - 8f, ripple.Position.Y - radius - 8f, (radius * 2f) + 16f, (radius * 2f) + 16f);
                continue;
            }

            if (_phase == GamePhase.Hunt && PlayerHasDirectSightTo(ripple.Position))
            {
                continue;
            }

            DrawDirectionalCue(graphics, ripple, progress, soundAlpha);
        }
    }

    private void DrawActors(Graphics graphics, PointF mousePosition)
    {
        if (_player.IsAlive)
        {
            DrawPlayerFov(graphics);
        }

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

        var points = new[]
        {
            new PointF(enemy.Position.X, enemy.Position.Y - enemy.Radius - 2f),
            new PointF(enemy.Position.X + enemy.Radius + 2f, enemy.Position.Y),
            new PointF(enemy.Position.X, enemy.Position.Y + enemy.Radius + 2f),
            new PointF(enemy.Position.X - enemy.Radius - 2f, enemy.Position.Y),
        };

        using var shadow = new SolidBrush(Color.FromArgb(82, 0, 0, 0));
        graphics.FillEllipse(shadow, enemy.Position.X - enemy.Radius - 5f, enemy.Position.Y - (enemy.Radius * 0.1f), (enemy.Radius * 2f) + 10f, enemy.Radius + 12f);
        using var ringPen = new Pen(Color.FromArgb(205, 236, 105, 90), 2f);
        using var glowPen = new Pen(Color.FromArgb(92, 255, 164, 112), 1.2f);
        graphics.DrawEllipse(ringPen, enemy.Position.X - enemy.Radius - 8f, enemy.Position.Y - 6f, (enemy.Radius * 2f) + 16f, (enemy.Radius * 1.18f) + 14f);
        graphics.DrawEllipse(glowPen, enemy.Position.X - enemy.Radius - 18f, enemy.Position.Y - 16f, (enemy.Radius * 2f) + 36f, (enemy.Radius * 2f) + 24f);

        using var fill = new SolidBrush(Color.FromArgb(235, 230, 95, 85));
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
        var weapon = _weaponStats[_player.Weapon];
        var fovDegrees = GetFovDegrees(_player.Weapon);
        var diameter = weapon.VisionRange * 2f;
        var startAngle = RadiansToDegrees(_player.FacingAngle) - (fovDegrees / 2f);

        using var path = new GraphicsPath();
        path.AddPie(_player.Position.X - weapon.VisionRange, _player.Position.Y - weapon.VisionRange, diameter, diameter, startAngle, fovDegrees);

        using var coneBrush = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(78, 248, 244, 214),
            SurroundColors = [Color.FromArgb(0, 120, 240, 255)],
        };

        graphics.FillPath(coneBrush, path);
    }

    private void DrawDirectionalCue(Graphics graphics, Ripple ripple, float progress, float soundAlpha)
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
        var color = Color.FromArgb(Math.Clamp(alpha, 18, 225), ripple.Color);
        var glow = Color.FromArgb(Math.Clamp(alpha / 3, 8, 72), ripple.Color);

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

    private void DrawHud(Graphics graphics)
    {
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

        DrawGhostHudText(graphics, "死角240度を、音で視る。", 18.5f, FontStyle.Bold, Color.FromArgb(255, 225, 245, 250), box.Left + 20, box.Top + 14);
        DrawHudText(graphics, "上段で戦況、左右で索敵情報、下段で装備とコマンドを確認できます。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 50);
        DrawHudText(graphics, "前半 4 ラウンドは防衛、後半は攻撃へ切り替わります。攻守交代時には再エディットが入ります。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 72);
        DrawHudText(graphics, "通常 120 度、SR は 100 度の視界です。投資 300 円付近が最効率で、ボスは同一人物を 2 回まで選出できます。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 94);
        DrawHudText(graphics, "構築中は Q/E で設置物スキン、R で会場広告テーマを変更できます。赤いスロットはノー・ビルドです。", 10.2f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 20, box.Top + 116);
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
        DrawCenteredHudText(graphics, $"第{_currentRound}ラウンド  |  {PlayerRoleLabel()}  |  SCORE {_playerRoundWins}-{_enemyRoundWins}{(_isOvertime ? " OT" : string.Empty)}  |  {ProfileSummaryLine()}", 8.3f, FontStyle.Bold, Color.FromArgb(236, 214, 224, 232), footer);
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
        DrawHudText(graphics, $"SKIN {SelectedStructureSkinName()} / AD {SelectedAdThemeName()}", 7.8f, FontStyle.Regular, Color.FromArgb(208, 198, 212, 222), IntelBounds.Left + 8, IntelBounds.Top + 162);

        DrawGhostHudText(graphics, "ログ", 8.8f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), IntelBounds.Left + 8, IntelBounds.Top + 186);
        using var feedFont = new Font(UiFontFamily, 8.4f, FontStyle.Regular);
        using var shadow = new SolidBrush(Color.FromArgb(176, 0, 0, 0));
        using var feedBrush = new SolidBrush(Color.FromArgb(236, 224, 232, 240));
        var lineY = IntelBounds.Top + 212f;
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
        DrawCenteredHudText(graphics, "ミニマップ", 12f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), new RectangleF(MinimapBounds.Left + 10, MinimapBounds.Top + 8, MinimapBounds.Width - 20, 18));

        var inner = Rectangle.Inflate(MinimapBounds, -10, -10);
        inner = new Rectangle(inner.Left, inner.Top + 22, inner.Width, inner.Height - 24);

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
                _ => Color.FromArgb(255, 180, 235, 120),
            };

            using var brush = new SolidBrush(color);
            if (!viewRect.Contains(center))
            {
                continue;
            }

            var point = new PointF(inner.Left + ((center.X - viewRect.Left) * scaleX), inner.Top + ((center.Y - viewRect.Top) * scaleY));
            graphics.FillEllipse(brush, point.X - 3.5f, point.Y - 3.5f, 7f, 7f);
        }

        DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, _player, Color.FromArgb(255, 90, 225, 245));
        foreach (var ally in _allies)
        {
            DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, ally, Color.FromArgb(255, 95, 225, 200));
        }

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && PlayerCanSee(actor)))
        {
            DrawMiniMapActor(graphics, inner, viewRect, scaleX, scaleY, enemy, Color.FromArgb(255, 235, 105, 90));
        }

        var playerPoint = new PointF(inner.Left + ((_player.Position.X - viewRect.Left) * scaleX), inner.Top + ((_player.Position.Y - viewRect.Top) * scaleY));
        var fovRadius = Math.Clamp(_weaponStats[_player.Weapon].VisionRange * scaleX * 0.55f, 18f, inner.Width * 0.48f);
        var fovDegrees = GetFovDegrees(_player.Weapon);
        var startAngle = RadiansToDegrees(_player.FacingAngle) - (fovDegrees / 2f);
        using (var fovPath = new GraphicsPath())
        using (var coneBrush = new SolidBrush(Color.FromArgb(56, 98, 228, 242)))
        {
            fovPath.AddPie(playerPoint.X - fovRadius, playerPoint.Y - fovRadius, fovRadius * 2f, fovRadius * 2f, startAngle, fovDegrees);
            graphics.FillPath(coneBrush, fovPath);
        }

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

        var core = BombSitePosition();
        using var coreBrush = new SolidBrush(_bombPlanted ? Color.FromArgb(255, 255, 128, 106) : Color.FromArgb(255, 78, 220, 195));
        if (viewRect.Contains(core))
        {
            var corePoint = new PointF(inner.Left + ((core.X - viewRect.Left) * scaleX), inner.Top + ((core.Y - viewRect.Top) * scaleY));
            graphics.FillEllipse(coreBrush, corePoint.X - 5f, corePoint.Y - 5f, 10f, 10f);
        }

        using var border = new Pen(Color.FromArgb(146, 194, 170, 110), 2.2f);
        graphics.DrawRectangle(border, inner);
    }

    private void DrawBottomBar(Graphics graphics)
    {
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
            var investDenominator = Math.Max(OptimalBossInvestment, Math.Max(1, AffordableCredits()));
            DrawLabeledBar(graphics, hpBar, "投資", _selectedBet / (float)investDenominator, Color.FromArgb(255, 238, 202, 112), Color.FromArgb(36, 8, 14, 18), $"{_selectedBet}c");
            DrawLabeledBar(graphics, shieldBar, "移動", Math.Clamp(BossMoveBonusPercent(_selectedBet) / 0.15f, 0f, 1f), Color.FromArgb(255, 92, 168, 232), Color.FromArgb(36, 8, 14, 18), $"+{BossMoveBonusPercent(_selectedBet) * 100f:0}%");
            DrawLabeledBar(graphics, sonicBar, "射撃", Math.Clamp(BossReloadBonusPercent(_selectedBet) / 0.22f, 0f, 1f), Color.FromArgb(255, 120, 214, 160), Color.FromArgb(36, 8, 14, 18), $"+{BossReloadBonusPercent(_selectedBet) * 100f:0}%");
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
            DrawAbilitySlot(graphics, abilityRect, "Enter", "アビリティ", "構築", false, Color.FromArgb(255, 208, 170, 104), 1, 1f, true);
        }
        else if (_phase == GamePhase.Bet)
        {
            DrawAbilitySlot(graphics, skillRects[0], "1", "ボス枠", "あなた", _selectedBossName == "あなた", Color.FromArgb(255, 116, 212, 230), BossSelectionsRemaining("あなた"), 1f, CanSelectBoss("あなた"));
            DrawAbilitySlot(graphics, skillRects[1], "2", "ボス枠", "北班", _selectedBossName == "北アンカー", Color.FromArgb(255, 164, 220, 116), BossSelectionsRemaining("北アンカー"), 1f, CanSelectBoss("北アンカー"));
            DrawAbilitySlot(graphics, skillRects[2], "3", "ボス枠", "南班", _selectedBossName == "南アンカー", Color.FromArgb(255, 230, 194, 88), BossSelectionsRemaining("南アンカー"), 1f, CanSelectBoss("南アンカー"));
            DrawAbilitySlot(graphics, abilityRect, "4", "ボス枠", "中班", _selectedBossName == "中央リンク", Color.FromArgb(255, 208, 170, 104), BossSelectionsRemaining("中央リンク"), 1f, CanSelectBoss("中央リンク"));
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
            DrawAbilitySlot(graphics, skillRects[0], "FOV", weapon.ShortLabel, $"{weapon.VisionClass}視界", false, WeaponAccent(weapon.Type), 1, Math.Clamp(weapon.VisionRange / 500f, 0f, 1f), true);
            DrawAbilitySlot(graphics, skillRects[1], "MAG", "弾数", $"{weapon.MagazineAmmo}/{weapon.ReserveAmmo}", false, Color.FromArgb(255, 116, 212, 230), 1, fireCharge, fireCharge >= 0.995f);
            DrawAbilitySlot(graphics, skillRects[2], "BOS", "投資", _player.IsBoss ? $"K {_roundBossKillCount} / +{BossMoveBonusPercent(_selectedBet) * 100f:0}%" : "非ボス", _player.IsBoss, Color.FromArgb(255, 164, 220, 116), _player.IsBoss ? Math.Max(1, _roundBossKillCount) : 0, BossInvestmentProgress(CurrentBossInvestment(_player)), _player.IsBoss);
            DrawAbilitySlot(graphics, abilityRect, IsPlayerTeamAttacking() && _bombPlanted ? "-" : "F", "アクション", CurrentSiteActionLabel(), false, Color.FromArgb(255, 208, 170, 104), 1, interactRatio, interactReady);
        }

        DrawLoadoutBox(graphics, mainWeaponRect, "メイン", WeaponLoadoutLabel(DisplayedWeaponType()));
        DrawLoadoutBox(graphics, subWeaponRect, "サブ", SidearmLoadoutLabel(DisplayedWeaponType()));
        DrawLoadoutBox(graphics, knifeRect, "ナイフ", "KNF");
        DrawCenteredHudText(graphics, CurrentControlsHint(), 7.6f, FontStyle.Bold, Color.FromArgb(234, 214, 224, 232), footerRect);
    }

    private void DrawInfoStatBox(Graphics graphics, Rectangle bounds, string title, string value, Color valueColor)
    {
        DrawCenteredHudText(graphics, title, 7.4f, FontStyle.Bold, Color.FromArgb(228, 214, 224, 232), new RectangleF(bounds.Left + 2, bounds.Top + 4, bounds.Width - 4, 10));
        DrawCenteredHudText(graphics, value, 9.8f, FontStyle.Bold, valueColor, new RectangleF(bounds.Left + 2, bounds.Top + 13, bounds.Width - 4, bounds.Height - 14));
    }

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

        if (actor.Type == ActorType.Player && IsPlayerBreathingExposed())
        {
            DrawEffectTag(graphics, new PointF(origin.X, origin.Y + offsetY), "呼吸漏", Color.FromArgb(255, 255, 132, 108));
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
