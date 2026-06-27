namespace RYZECHo;

public readonly record struct Size(int Width, int Height);

public readonly record struct Point(int X, int Y)
{
    public static Point Empty { get; } = new(0, 0);

    public static implicit operator PointF(Point point) => new(point.X, point.Y);
}

public record struct PointF(float X, float Y)
{
    public static PointF Empty { get; } = new(0f, 0f);
}

public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public Size Size => new(Width, Height);

    public bool Contains(Point point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public bool Contains(PointF point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public bool IntersectsWith(Rectangle other) =>
        other.Left < Right && Left < other.Right && other.Top < Bottom && Top < other.Bottom;

    public bool IntersectsWith(RectangleF other) =>
        other.Left < Right && Left < other.Right && other.Top < Bottom && Top < other.Bottom;

    public static Rectangle Inflate(Rectangle rectangle, int width, int height) =>
        new(rectangle.X - width, rectangle.Y - height, rectangle.Width + (width * 2), rectangle.Height + (height * 2));

    public static Rectangle Round(RectangleF rectangle) =>
        new(
            (int)MathF.Round(rectangle.X),
            (int)MathF.Round(rectangle.Y),
            (int)MathF.Round(rectangle.Width),
            (int)MathF.Round(rectangle.Height));

    public static implicit operator RectangleF(Rectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
}

public record struct RectangleF(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Size Size => new((int)MathF.Round(Width), (int)MathF.Round(Height));

    public bool Contains(PointF point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public bool Contains(Point point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public bool IntersectsWith(RectangleF other) =>
        other.Left < Right && Left < other.Right && other.Top < Bottom && Top < other.Bottom;

    public bool IntersectsWith(Rectangle other) =>
        other.Left < Right && Left < other.Right && other.Top < Bottom && Top < other.Bottom;

    public void Offset(float dx, float dy)
    {
        X += dx;
        Y += dy;
    }

    public static RectangleF Inflate(RectangleF rectangle, float width, float height) =>
        new(rectangle.X - width, rectangle.Y - height, rectangle.Width + (width * 2f), rectangle.Height + (height * 2f));

    public static RectangleF Inflate(Rectangle rectangle, float width, float height) =>
        Inflate((RectangleF)rectangle, width, height);
}

public readonly record struct Color(byte A, byte R, byte G, byte B)
{
    public static Color Transparent { get; } = FromArgb(0, 0, 0, 0);
    public static Color CornflowerBlue { get; } = FromArgb(255, 100, 149, 237);
    public static Color White { get; } = FromArgb(255, 255, 255, 255);
    public static Color clear => Transparent;
    public static Color white => White;

    public Color(float red, float green, float blue)
        : this(red, green, blue, 1f)
    {
    }

    public Color(float red, float green, float blue, float alpha)
        : this(ClampUnit(alpha), ClampUnit(red), ClampUnit(green), ClampUnit(blue))
    {
    }

    public float a => A / 255f;
    public float r => R / 255f;
    public float g => G / 255f;
    public float b => B / 255f;

    public static Color FromArgb(int red, int green, int blue) => FromArgb(255, red, green, blue);

    public static Color FromArgb(int alpha, int red, int green, int blue) =>
        new(ClampByte(alpha), ClampByte(red), ClampByte(green), ClampByte(blue));

    public static Color FromArgb(int alpha, Color baseColor) =>
        new(ClampByte(alpha), baseColor.R, baseColor.G, baseColor.B);

    public UnityEngine.Color32 ToUnityColor() => new(R, G, B, A);

    public static implicit operator UnityEngine.Color32(Color color) => color.ToUnityColor();
    public static implicit operator UnityEngine.Color(Color color) => new(color.r, color.g, color.b, color.a);
    public static implicit operator Color(UnityEngine.Color color) => new(color.r, color.g, color.b, color.a);

    public static Color Lerp(Color start, Color end, float t)
    {
        var clamped = Math.Clamp(t, 0f, 1f);
        return new Color(
            start.r + ((end.r - start.r) * clamped),
            start.g + ((end.g - start.g) * clamped),
            start.b + ((end.b - start.b) * clamped),
            start.a + ((end.a - start.a) * clamped));
    }

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);

    private static byte ClampUnit(float value) => ClampByte((int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f));
}
