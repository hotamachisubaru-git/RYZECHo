#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER

namespace RYZECHo;

internal sealed class Matrix : IDisposable
{
    public Matrix()
        : this(1f, 0f, 0f, 1f, 0f, 0f)
    {
    }

    public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
    {
        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
        DX = dx;
        DY = dy;
    }

    public float M11 { get; private set; }
    public float M12 { get; private set; }
    public float M21 { get; private set; }
    public float M22 { get; private set; }
    public float DX { get; private set; }
    public float DY { get; private set; }

    public Matrix Clone() => new(M11, M12, M21, M22, DX, DY);

    public void Translate(float offsetX, float offsetY, MatrixOrder order = MatrixOrder.Prepend) =>
        Multiply(new Matrix(1f, 0f, 0f, 1f, offsetX, offsetY), order);

    public void Scale(float scaleX, float scaleY, MatrixOrder order = MatrixOrder.Prepend) =>
        Multiply(new Matrix(scaleX, 0f, 0f, scaleY, 0f, 0f), order);

    public void Rotate(float degrees, MatrixOrder order = MatrixOrder.Prepend)
    {
        var radians = degrees * (MathF.PI / 180f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        Multiply(new Matrix(cos, sin, -sin, cos, 0f, 0f), order);
    }

    public void Multiply(Matrix matrix, MatrixOrder order = MatrixOrder.Prepend)
    {
        var result = order == MatrixOrder.Append
            ? MultiplyCore(this, matrix)
            : MultiplyCore(matrix, this);

        M11 = result.M11;
        M12 = result.M12;
        M21 = result.M21;
        M22 = result.M22;
        DX = result.DX;
        DY = result.DY;
    }

    public void Invert()
    {
        var determinant = (M11 * M22) - (M12 * M21);
        if (MathF.Abs(determinant) < 0.000001f)
        {
            return;
        }

        var invDet = 1f / determinant;
        var m11 = M22 * invDet;
        var m12 = -M12 * invDet;
        var m21 = -M21 * invDet;
        var m22 = M11 * invDet;
        var dx = ((M21 * DY) - (DX * M22)) * invDet;
        var dy = ((DX * M12) - (M11 * DY)) * invDet;

        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
        DX = dx;
        DY = dy;
    }

    public void TransformPoints(PointF[] points)
    {
        for (var index = 0; index < points.Length; index++)
        {
            points[index] = Transform(points[index]);
        }
    }

    public PointF Transform(PointF point) =>
        new(
            (point.X * M11) + (point.Y * M21) + DX,
            (point.X * M12) + (point.Y * M22) + DY);

    public void Dispose()
    {
    }

    private static Matrix MultiplyCore(Matrix left, Matrix right) =>
        new(
            (left.M11 * right.M11) + (left.M12 * right.M21),
            (left.M11 * right.M12) + (left.M12 * right.M22),
            (left.M21 * right.M11) + (left.M22 * right.M21),
            (left.M21 * right.M12) + (left.M22 * right.M22),
            (left.DX * right.M11) + (left.DY * right.M21) + right.DX,
            (left.DX * right.M12) + (left.DY * right.M22) + right.DY);
}

internal sealed class GraphicsState(Matrix transform)
{
    public Matrix Transform { get; } = transform;
}


#endif
