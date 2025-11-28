using System;
using System.Numerics;

namespace DeepDungeonRadar.Utils;

internal static class VectorExtensions
{
    public static Vector2 ToVector2(this Vector3 v) => new(v.X, v.Z);

    public static Vector3 ToVector3(this Vector2 vin) => new(vin.X, 0f, vin.Y);

    public static float Distance(this Vector3 v, Vector3 v2) => Vector3.Distance(v, v2);

    public static float Distance(this Vector2 v, Vector2 v2) => Vector2.Distance(v, v2);
    
    public static float Distance2D(this Vector3 v, Vector3 v2) => (v - v2).ToVector2().Length();

    public static Vector2 Normalize(this Vector2 v) => Vector2.Normalize(v);

    public static Vector2 Zoom(this Vector2 vin, float zoom, Vector2 origin = default) => origin + (vin - origin) * zoom;

    public static Vector2 RotateAround(this Vector2 vin, Vector2 pivot, float rad)
        => vin.RotateAround(pivot, rad.ToNormalizedVector2());

    // rotation must be a normalized vector
    public static Vector2 RotateAround(this Vector2 vin, Vector2 pivot, Vector2 rotation)
    {
        var diff = vin - pivot;
        return pivot + new Vector2(rotation.Y * diff.X - rotation.X * diff.Y, rotation.Y * diff.Y + rotation.X * diff.X);
    }

    public static Vector2 ToNormalizedVector2(this float rad) => new(MathF.Sin(rad), MathF.Cos(rad));

    public static Vector2 WorldToWindow(this Vector2 objWorldPos, Vector2 pivotWorldPos, Vector2 pivotWindowPos, float zoom, Vector2 rotation)
         => pivotWindowPos + ((objWorldPos - pivotWorldPos) * zoom).RotateAround(default, rotation);
}
