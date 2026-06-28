#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace RYZECHo;

internal sealed partial class Graphics
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly BasicEffect _effect;
    private readonly Texture2D _pixel;
    private readonly FontSystem _fontSystem;
    private readonly Dictionary<int, DynamicSpriteFont> _fontCache = [];
    private readonly Stack<Matrix> _transformStack = [];
    private Matrix _transform = new();
    private bool _spriteBatchActive;
    private bool _fontAvailable;

    public Graphics(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            TextureEnabled = false,
        };

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([XnaColor.White]);

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            TextureWidth = 2048,
            TextureHeight = 2048,
        });
        LoadSystemFont();
    }

    public void BeginFrame()
    {
        _transform = new Matrix();
        _transformStack.Clear();
        UpdateProjection();
    }

    public void EndFrame()
    {
        EndSpriteBatch();
    }

    public GraphicsState Save()
    {
        var snapshot = _transform.Clone();
        _transformStack.Push(snapshot);
        return new GraphicsState(snapshot.Clone());
    }

    public void Restore(GraphicsState state)
    {
        _transform = _transformStack.Count > 0 ? _transformStack.Pop() : state.Transform.Clone();
    }

    public void MultiplyTransform(Matrix matrix)
    {
        _transform.Multiply(matrix, MatrixOrder.Append);
    }

    public void TranslateTransform(float dx, float dy)
    {
        _transform.Translate(dx, dy, MatrixOrder.Prepend);
    }

    public void RotateTransform(float degrees)
    {
        _transform.Rotate(degrees, MatrixOrder.Prepend);
    }

    public void FillRectangle(Brush brush, Rectangle rectangle) => FillRectangle(brush, (RectangleF)rectangle);

    public void FillRectangle(Brush brush, RectangleF rectangle)
    {
        var points = RectanglePoints(rectangle);
        DrawFilledPolygon(points, brush.Color);
    }

    public void FillRectangle(Brush brush, float x, float y, float width, float height) =>
        FillRectangle(brush, new RectangleF(x, y, width, height));

    public void DrawRectangle(Pen pen, Rectangle rectangle) => DrawRectangle(pen, (RectangleF)rectangle);

    public void DrawRectangle(Pen pen, RectangleF rectangle)
    {
        var points = RectanglePoints(rectangle);
        DrawPolyline(points, pen, closed: true);
    }

    public void DrawRectangle(Pen pen, float x, float y, float width, float height) =>
        DrawRectangle(pen, new RectangleF(x, y, width, height));

    public void FillEllipse(Brush brush, Rectangle rectangle) => FillEllipse(brush, (RectangleF)rectangle);

    public void FillEllipse(Brush brush, RectangleF rectangle)
    {
        DrawFilledPolygon(EllipsePoints(rectangle, 42), brush.Color);
    }

    public void FillEllipse(Brush brush, float x, float y, float width, float height) =>
        FillEllipse(brush, new RectangleF(x, y, width, height));

    public void DrawEllipse(Pen pen, Rectangle rectangle) => DrawEllipse(pen, (RectangleF)rectangle);

    public void DrawEllipse(Pen pen, RectangleF rectangle)
    {
        DrawPolyline(EllipsePoints(rectangle, 48), pen, closed: true);
    }

    public void DrawEllipse(Pen pen, float x, float y, float width, float height) =>
        DrawEllipse(pen, new RectangleF(x, y, width, height));

    public void DrawLine(Pen pen, PointF start, PointF end) => DrawLine(pen, start.X, start.Y, end.X, end.Y);

    public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
    {
        DrawLineTransformed(pen, Transform(new PointF(x1, y1)), Transform(new PointF(x2, y2)));
    }

    public void DrawLines(Pen pen, PointF[] points) => DrawPolyline(points, pen, closed: false);

    public void FillPolygon(Brush brush, Point[] points) =>
        FillPolygon(brush, points.Select(point => (PointF)point).ToArray());

    public void FillPolygon(Brush brush, PointF[] points)
    {
        DrawFilledPolygon(points, brush.Color);
    }

    public void DrawPolygon(Pen pen, Point[] points) =>
        DrawPolygon(pen, points.Select(point => (PointF)point).ToArray());

    public void DrawPolygon(Pen pen, PointF[] points)
    {
        DrawPolyline(points, pen, closed: true);
    }

    public void FillPath(Brush brush, GraphicsPath path)
    {
        DrawFilledPolygon(path.Points, brush.Color);
    }

    public void DrawPath(Pen pen, GraphicsPath path)
    {
        DrawPolyline(path.Points, pen, path.Closed);
    }

    public void DrawBezier(Pen pen, PointF p0, PointF p1, PointF p2, PointF p3)
    {
        const int segments = 28;
        var points = new PointF[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var inv = 1f - t;
            points[i] = new PointF(
                (inv * inv * inv * p0.X) + (3f * inv * inv * t * p1.X) + (3f * inv * t * t * p2.X) + (t * t * t * p3.X),
                (inv * inv * inv * p0.Y) + (3f * inv * inv * t * p1.Y) + (3f * inv * t * t * p2.Y) + (t * t * t * p3.Y));
        }

        DrawPolyline(points, pen, closed: false);
    }

    public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        var segments = Math.Max(8, (int)MathF.Ceiling(MathF.Abs(sweepAngle) / 6f));
        var points = new PointF[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var degrees = startAngle + ((sweepAngle / segments) * i);
            var radians = degrees * (MathF.PI / 180f);
            points[i] = new PointF(
                x + (width / 2f) + (MathF.Cos(radians) * width / 2f),
                y + (height / 2f) + (MathF.Sin(radians) * height / 2f));
        }

        DrawPolyline(points, pen, closed: false);
    }

    public void DrawString(string text, Font font, Brush brush, float x, float y)
    {
        DrawTextLines(text, font, brush.Color, new RectangleF(x, y, float.MaxValue, float.MaxValue), null);
    }

    public void DrawString(string text, Font font, Brush brush, RectangleF bounds)
    {
        DrawTextLines(text, font, brush.Color, bounds, null);
    }

    public void DrawString(string text, Font font, Brush brush, RectangleF bounds, StringFormat format)
    {
        DrawTextLines(text, font, brush.Color, bounds, format);
    }

    public void Dispose()
    {
        EndFrame();
        _pixel.Dispose();
        _effect.Dispose();
        _fontSystem.Dispose();
    }

}


#endif
