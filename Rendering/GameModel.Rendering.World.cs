namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
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
            "ARC LEAGUE" => new[] { "ARC LEAGUE", "CREATOR MARKET", "COSMETIC ONLY" },
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
                case StructureKind.ReconBeacon:
                    using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 6, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(Color.FromArgb(172, 112, 220, 252)))
                    using (var pen = new Pen(Color.FromArgb(255, 210, 246, 255), 2f))
                    using (var pingPen = new Pen(Color.FromArgb(92, 122, 228, 255), 1.4f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                        graphics.DrawEllipse(pingPen, rectangle.Left - 18, rectangle.Top - 18, rectangle.Width + 36, rectangle.Height + 36);
                    }
                    break;
                case StructureKind.ShieldRelay:
                    using (var shadow = new SolidBrush(Color.FromArgb(68, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 8, rectangle.Top + 16, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(Color.FromArgb(164, 104, 236, 168)))
                    using (var pen = new Pen(Color.FromArgb(255, 218, 255, 232), 2.2f))
                    {
                        graphics.FillRectangle(fill, rectangle);
                        graphics.DrawRectangle(pen, rectangle);
                    }

                    using (var arcPen = new Pen(Color.FromArgb(92, 180, 255, 214), 1.4f))
                    {
                        graphics.DrawArc(arcPen, rectangle.Left - 16, rectangle.Top - 12, rectangle.Width + 32, rectangle.Height + 24, 210f, 120f);
                        graphics.DrawArc(arcPen, rectangle.Left - 16, rectangle.Top - 12, rectangle.Width + 32, rectangle.Height + 24, 30f, 120f);
                    }

                    var relayRatio = structure.Health / structure.MaxHealth;
                    using (var hpBack = new SolidBrush(Color.FromArgb(36, 0, 0, 0)))
                    using (var hpFill = new SolidBrush(Color.FromArgb(220, 142, 255, 194)))
                    {
                        var hpRect = new RectangleF(rectangle.Left, rectangle.Bottom + 3f, rectangle.Width, 5f);
                        graphics.FillRectangle(hpBack, hpRect);
                        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * relayRatio, hpRect.Height);
                    }
                    break;
                case StructureKind.PortableCover:
                    DrawRaisedBlock(graphics, rectangle, Color.FromArgb(142, 126, 146, 164), Color.FromArgb(64, 24, 30, 38), Color.FromArgb(232, 226, 238, 246), 10f);
                    DrawStructureHealthBar(graphics, rectangle, structure, Color.FromArgb(220, 226, 238, 246));
                    break;
                case StructureKind.VisorWall:
                    using (var wallFill = new LinearGradientBrush(rectangle, Color.FromArgb(118, 82, 108, 190), Color.FromArgb(62, 24, 38, 74), 90f))
                    using (var wallBorder = new Pen(Color.FromArgb(238, 174, 206, 255), 2f))
                    using (var scanPen = new Pen(Color.FromArgb(130, 132, 228, 255), 1.2f))
                    {
                        graphics.FillRectangle(wallFill, rectangle);
                        graphics.DrawRectangle(wallBorder, rectangle);
                        graphics.DrawLine(scanPen, rectangle.Left + 5, rectangle.Top + 8, rectangle.Right - 5, rectangle.Top + 8);
                        graphics.DrawLine(scanPen, rectangle.Left + 5, rectangle.Bottom - 8, rectangle.Right - 5, rectangle.Bottom - 8);
                    }

                    DrawStructureHealthBar(graphics, rectangle, structure, Color.FromArgb(220, 174, 206, 255));
                    break;
                case StructureKind.HoloDecoy:
                    using (var shadow = new SolidBrush(Color.FromArgb(52, 0, 0, 0)))
                    using (var fill = new SolidBrush(Color.FromArgb(84, 196, 132, 255)))
                    using (var border = new Pen(Color.FromArgb(224, 226, 206, 255), 1.8f))
                    using (var ghost = new Pen(Color.FromArgb(104, 196, 132, 255), 1.2f))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 8, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(border, rectangle);
                        graphics.DrawLine(ghost, rectangle.Left + 10, rectangle.Top + 8, rectangle.Right - 10, rectangle.Bottom - 8);
                        graphics.DrawLine(ghost, rectangle.Right - 10, rectangle.Top + 8, rectangle.Left + 10, rectangle.Bottom - 8);
                    }
                    break;
            }
        }
    }

    private static void DrawStructureHealthBar(Graphics graphics, Rectangle rectangle, Structure structure, Color fillColor)
    {
        var ratio = structure.MaxHealth <= 0f ? 0f : Math.Clamp(structure.Health / structure.MaxHealth, 0f, 1f);
        using var hpBack = new SolidBrush(Color.FromArgb(36, 0, 0, 0));
        using var hpFill = new SolidBrush(fillColor);
        var hpRect = new RectangleF(rectangle.Left, rectangle.Bottom + 3f, rectangle.Width, 5f);
        graphics.FillRectangle(hpBack, hpRect);
        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
    }

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
            var soundAlpha = GetSoundAlphaMultiplier(ripple.Position);
            var sharedOnly = _phase == GamePhase.Hunt && !PlayerCanPerceive(ripple.Position, ripple.Strength);

            if (ripple.Kind is RippleKind.Footstep or RippleKind.Breathing)
            {
                var isBreathing = ripple.Kind == RippleKind.Breathing;
                var radius = isBreathing
                    ? 10f + (progress * 44f * ripple.Strength)
                    : 16f + (progress * 84f * ripple.Strength);
                var baseAlpha = isBreathing ? 86f : sharedOnly ? 92f : 150f;
                var alpha = (int)(baseAlpha * (1f - progress) * soundAlpha);
                var baseColor = sharedOnly ? Color.FromArgb(180, 124, 214, 255) : ripple.Color;
                var color = Color.FromArgb(Math.Clamp(alpha, isBreathing ? 8 : 12, isBreathing ? 112 : 165), baseColor);

                using var pen = new Pen(color, isBreathing ? 1.3f : 2f);
                using var halo = new Pen(Color.FromArgb(Math.Clamp(alpha / 2, 6, isBreathing ? 48 : 80), baseColor), 1f);
                graphics.DrawEllipse(pen, ripple.Position.X - radius, ripple.Position.Y - radius, radius * 2f, radius * 2f);
                graphics.DrawEllipse(halo, ripple.Position.X - radius - 8f, ripple.Position.Y - radius - 8f, (radius * 2f) + 16f, (radius * 2f) + 16f);
                continue;
            }

            if (_phase == GamePhase.Hunt && PlayerHasDirectSightTo(ripple.Position))
            {
                continue;
            }

            DrawDirectionalCue(graphics, ripple, progress, soundAlpha, sharedOnly);
        }
    }

}
