#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

using Microsoft.Xna.Framework.Graphics;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace RYZECHo;

internal sealed partial class Graphics
{
    private void DrawFilledPolygon(IReadOnlyList<PointF> points, Color color)
    {
        if (points.Count < 3)
        {
            return;
        }

        EndSpriteBatch();
        EnsurePrimitiveState();

        var vertices = new VertexPositionColor[(points.Count - 2) * 3];
        var first = Transform(points[0]);
        var vertexIndex = 0;
        for (var index = 1; index < points.Count - 1; index++)
        {
            vertices[vertexIndex++] = Vertex(first, color);
            vertices[vertexIndex++] = Vertex(Transform(points[index]), color);
            vertices[vertexIndex++] = Vertex(Transform(points[index + 1]), color);
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
        }
    }

    private void DrawPolyline(IReadOnlyList<PointF> points, Pen pen, bool closed)
    {
        if (points.Count < 2)
        {
            return;
        }

        for (var index = 0; index < points.Count - 1; index++)
        {
            DrawLineTransformed(pen, Transform(points[index]), Transform(points[index + 1]));
        }

        if (closed)
        {
            DrawLineTransformed(pen, Transform(points[^1]), Transform(points[0]));
        }
    }

    private void DrawLineTransformed(Pen pen, PointF start, PointF end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0.01f)
        {
            return;
        }

        EndSpriteBatch();
        EnsurePrimitiveState();

        var halfWidth = Math.Max(1f, pen.Width) / 2f;
        var nx = -(dy / length) * halfWidth;
        var ny = (dx / length) * halfWidth;
        var color = pen.Color;

        var vertices = new[]
        {
            Vertex(new PointF(start.X + nx, start.Y + ny), color),
            Vertex(new PointF(end.X + nx, end.Y + ny), color),
            Vertex(new PointF(end.X - nx, end.Y - ny), color),
            Vertex(new PointF(start.X + nx, start.Y + ny), color),
            Vertex(new PointF(end.X - nx, end.Y - ny), color),
            Vertex(new PointF(start.X - nx, start.Y - ny), color),
        };

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
        }
    }

    private PointF Transform(PointF point) => _transform.Transform(point);

    private static PointF[] RectanglePoints(RectangleF rectangle) =>
    [
        new(rectangle.Left, rectangle.Top),
        new(rectangle.Right, rectangle.Top),
        new(rectangle.Right, rectangle.Bottom),
        new(rectangle.Left, rectangle.Bottom),
    ];

    private static PointF[] EllipsePoints(RectangleF rectangle, int segments)
    {
        var points = new PointF[segments];
        var centerX = rectangle.Left + (rectangle.Width / 2f);
        var centerY = rectangle.Top + (rectangle.Height / 2f);
        for (var index = 0; index < segments; index++)
        {
            var radians = (MathF.PI * 2f * index) / segments;
            points[index] = new PointF(
                centerX + (MathF.Cos(radians) * rectangle.Width / 2f),
                centerY + (MathF.Sin(radians) * rectangle.Height / 2f));
        }

        return points;
    }

    private static VertexPositionColor Vertex(PointF point, Color color) =>
        new(new XnaVector3(point.X, point.Y, 0f), color.ToXnaColor());

    private void EnsurePrimitiveState()
    {
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        UpdateProjection();
    }

    private void BeginSpriteBatch()
    {
        if (_spriteBatchActive)
        {
            return;
        }

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _spriteBatchActive = true;
    }

    private void EndSpriteBatch()
    {
        if (!_spriteBatchActive)
        {
            return;
        }

        _spriteBatch.End();
        _spriteBatchActive = false;
    }

    private void UpdateProjection()
    {
        var viewport = _graphicsDevice.Viewport;
        _effect.Projection = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(
            0,
            viewport.Width,
            viewport.Height,
            0,
            0,
            1);
    }

    private void LoadSystemFont()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "NotoSansJP-VF.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "NotoSansJP-VF.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "NotoSans-Regular.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                _fontSystem.AddFont(File.ReadAllBytes(candidate));
                _fontAvailable = true;
                return;
            }
            catch
            {
                _fontAvailable = false;
            }
        }
    }
}


#endif
