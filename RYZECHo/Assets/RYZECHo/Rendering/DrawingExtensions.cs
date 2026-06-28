#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
using UnityEngine;
using System.Drawing;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;

namespace RYZECHo
{
    /// <summary>
    /// UnityEngine と System.Drawing の間で型変換するための拡張メソッド。
    /// </summary>
    public static class DrawingExtensions
    {
        public static Point ToPoint(this Vector2 vector) => new Point((int)vector.x, (int)vector.y);
        public static Point ToPoint(this Vector3 vector) => new Point((int)vector.x, (int)vector.y);
        public static Vector2 ToVector2(this Point point) => new Vector2(point.X, point.Y);
        public static Vector2 ToVector2(this PointF pointF) => new Vector2(pointF.X, pointF.Y);
        public static Rectangle ToRectangle(this Rect rect) => new Rectangle(rect.x, rect.y, rect.width, rect.height);
        public static Rect ToRect(this Rectangle rectangle) => new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }
}
#endif
