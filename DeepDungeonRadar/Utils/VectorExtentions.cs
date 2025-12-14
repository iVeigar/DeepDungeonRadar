using System.Numerics;
namespace DeepDungeonRadar.Utils;

internal static class VectorExtensions
{
    public static Vector2 Transform(this Vector2 v, Matrix3x2 matrix) => Vector2.Transform(v, matrix);

    public static bool InRect(this Vector2 p, Vector2 vmin, Vector2 vmax) => p.X >= vmin.X && p.Y >= vmin.Y && p.X <= vmax.X && p.Y <= vmax.Y;
}
