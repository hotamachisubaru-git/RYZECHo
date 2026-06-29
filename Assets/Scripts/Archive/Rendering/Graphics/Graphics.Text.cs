#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

using FontStashSharp;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace RYZECHo;

internal sealed partial class Graphics
{
    private void DrawTextLines(string text, Font font, Color color, RectangleF bounds, StringFormat? format)
    {
        if (!_fontAvailable || string.IsNullOrEmpty(text))
        {
            return;
        }

        BeginSpriteBatch();
        var spriteFont = GetFont(font);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var lineHeight = Math.Max(spriteFont.LineHeight, font.Size + 2f);
        var totalHeight = lineHeight * lines.Length;
        var startY = bounds.Top;

        if (format?.LineAlignment == StringAlignment.Center && bounds.Height < float.MaxValue / 2f)
        {
            startY = bounds.Top + ((bounds.Height - totalHeight) / 2f);
        }

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var measured = spriteFont.MeasureString(line);
            var x = bounds.Left;
            if (format?.Alignment == StringAlignment.Center && bounds.Width < float.MaxValue / 2f)
            {
                x = bounds.Left + ((bounds.Width - measured.X) / 2f);
            }
            else if (format?.Alignment == StringAlignment.Far && bounds.Width < float.MaxValue / 2f)
            {
                x = bounds.Right - measured.X;
            }

            var position = Transform(new PointF(x, startY + (index * lineHeight)));
            var rotation = MathF.Atan2(_transform.M12, _transform.M11);
            var scale = MathF.Sqrt((_transform.M11 * _transform.M11) + (_transform.M12 * _transform.M12));
            if (!float.IsFinite(scale) || scale <= 0.001f)
            {
                scale = 1f;
            }

            _spriteBatch.DrawString(
                spriteFont,
                line,
                new XnaVector2(position.X, position.Y),
                color.ToXnaColor(),
                rotation,
                XnaVector2.Zero,
                new XnaVector2(scale, scale),
                0f,
                0f);
        }
    }

    private DynamicSpriteFont GetFont(Font font)
    {
        var size = Math.Clamp((int)MathF.Round(font.Size * 1.35f), 8, 48);
        if (!_fontCache.TryGetValue(size, out var spriteFont))
        {
            spriteFont = _fontSystem.GetFont(size);
            _fontCache[size] = spriteFont;
        }

        return spriteFont;
    }
}


#endif
