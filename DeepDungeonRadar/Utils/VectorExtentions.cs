using System;
using System.Numerics;
namespace DeepDungeonRadar.Utils;

internal static class VectorExtensions
{
    public static Vector2 ToVector2(this Vector3 v) => new(v.X, v.Z);

    public static Vector3 ToVector3(this Vector2 v) => new(v.X, 0f, v.Y);

    public static float Distance(this Vector3 v, Vector3 v2) => Vector3.Distance(v, v2);

    public static float Distance(this Vector2 v, Vector2 v2) => Vector2.Distance(v, v2);
    
    public static float Distance2D(this Vector3 v, Vector3 v2) => (v - v2).ToVector2().Length();

    public static Vector2 Scale(this Vector2 v, Vector2 scale, Vector2 origin = default) => origin + (v - origin) * scale;

    public static Vector2 Scale(this Vector2 v, float scale, Vector2 origin = default) => origin + (v - origin) * scale;

    public static float ToRotation(this Vector2 dir) => MathF.Atan2(dir.X, dir.Y);

    public static Vector2 ToDirection(this float rot)
    {
        var (sin, cos) = MathF.SinCos(rot);
        return new(sin, cos);
    }

    public static Vector2 ToDirection(this Vector2 v) => Vector2.Normalize(v);

    public static Vector2 Rotate(this Vector2 v, float rot, Vector2 pivot = default)
        => v.Rotate(rot.ToDirection(), pivot);

    // rotation must be a normalized vector
    public static Vector2 Rotate(this Vector2 v, Vector2 dir, Vector2 pivot = default)
    {
        var x = v.X - pivot.X;
        var y = v.Y - pivot.Y;
        return pivot + new Vector2(x * dir.Y + y * dir.X, y * dir.Y - x * dir.X);
    }

    public static Vector2 Transform(this Vector2 v, Matrix3x2 matrix) => Vector2.Transform(v, matrix);

    public static bool InRect(this Vector2 p, Vector2 vmin, Vector2 vmax) => p.X >= vmin.X && p.Y >= vmin.Y && p.X <= vmax.X && p.Y <= vmax.Y;
}
