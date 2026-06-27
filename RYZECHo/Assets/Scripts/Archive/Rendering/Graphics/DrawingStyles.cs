namespace RYZECHo;

internal enum FontStyle
{
    Regular,
    Bold,
}

internal enum GraphicsUnit
{
    Point,
}

internal enum StringAlignment
{
    Near,
    Center,
    Far,
}

internal enum StringTrimming
{
    None,
    EllipsisCharacter,
}

internal enum DashStyle
{
    Solid,
    Dash,
}

internal enum LineCap
{
    Flat,
    Round,
}

internal enum LineJoin
{
    Miter,
    Round,
}

internal enum MatrixOrder
{
    Prepend,
    Append,
}

internal sealed class Font(string family, float size, FontStyle style = FontStyle.Regular) : IDisposable
{
    public string Family { get; } = family;
    public float Size { get; } = size;
    public FontStyle Style { get; } = style;

    public void Dispose()
    {
    }
}

internal sealed class StringFormat : IDisposable
{
    public StringAlignment Alignment { get; set; } = StringAlignment.Near;
    public StringAlignment LineAlignment { get; set; } = StringAlignment.Near;
    public StringTrimming Trimming { get; set; } = StringTrimming.None;

    public void Dispose()
    {
    }
}

internal abstract class Brush(Color color) : IDisposable
{
    public virtual Color Color { get; protected set; } = color;

    public virtual void Dispose()
    {
    }
}

internal sealed class SolidBrush(Color color) : Brush(color);

internal sealed class LinearGradientBrush : Brush
{
    public LinearGradientBrush(Rectangle bounds, Color startColor, Color endColor, float angle)
        : this((RectangleF)bounds, startColor, endColor, angle)
    {
    }

    public LinearGradientBrush(RectangleF bounds, Color startColor, Color endColor, float angle)
        : base(Blend(startColor, endColor, 0.5f))
    {
        Bounds = bounds;
        StartColor = startColor;
        EndColor = endColor;
        Angle = angle;
    }

    public RectangleF Bounds { get; }
    public Color StartColor { get; }
    public Color EndColor { get; }
    public float Angle { get; }

    private static Color Blend(Color left, Color right, float amount)
    {
        static byte Lerp(byte a, byte b, float t) => (byte)Math.Clamp((int)MathF.Round(a + ((b - a) * t)), 0, 255);
        return new Color(
            Lerp(left.A, right.A, amount),
            Lerp(left.R, right.R, amount),
            Lerp(left.G, right.G, amount),
            Lerp(left.B, right.B, amount));
    }
}

internal sealed class PathGradientBrush(GraphicsPath path) : Brush(Color.Transparent)
{
    public GraphicsPath Path { get; } = path;
    public Color CenterColor { get; set; } = Color.White;
    public Color[] SurroundColors { get; set; } = [];

    public override Color Color
    {
        get => CenterColor;
        protected set => CenterColor = value;
    }
}

internal sealed class Pen(Color color, float width = 1f) : IDisposable
{
    public Color Color { get; } = color;
    public float Width { get; } = width;
    public DashStyle DashStyle { get; set; } = DashStyle.Solid;
    public LineCap StartCap { get; set; } = LineCap.Flat;
    public LineCap EndCap { get; set; } = LineCap.Flat;
    public LineJoin LineJoin { get; set; } = LineJoin.Miter;

    public void Dispose()
    {
    }
}

internal sealed class GraphicsPath : IDisposable
{
    private readonly List<PointF> _points = [];

    public IReadOnlyList<PointF> Points => _points;
    public bool Closed { get; private set; }

    public void AddPolygon(PointF[] points)
    {
        _points.Clear();
        _points.AddRange(points);
        Closed = true;
    }

    public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        _points.Clear();
        _points.Add(new PointF(x + (width / 2f), y + (height / 2f)));

        var segments = Math.Max(10, (int)MathF.Ceiling(MathF.Abs(sweepAngle) / 7.5f));
        for (var index = 0; index <= segments; index++)
        {
            var degrees = startAngle + ((sweepAngle / segments) * index);
            var radians = degrees * (MathF.PI / 180f);
            _points.Add(new PointF(
                x + (width / 2f) + (MathF.Cos(radians) * width / 2f),
                y + (height / 2f) + (MathF.Sin(radians) * height / 2f)));
        }

        Closed = true;
    }

    public void Dispose()
    {
    }
}
