#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
namespace RYZECHo;

internal sealed partial class GameModel
{
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
}
#endif
