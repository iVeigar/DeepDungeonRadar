using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using DeepDungeonRadar.Enums;
using ImGuiScene;
using Newtonsoft.Json;

namespace DeepDungeonRadar.Misc;

internal static class Utils
{

    public static Vector2 ToVector2(this Vector3 v) => new(v.X, v.Z);

    public static Vector3 ToVector3(this Vector2 vin) => new(vin.X, 0f, vin.Y);

    public static float Distance(this Vector3 v, Vector3 v2) => Vector3.Distance(v, v2);

    public static float Distance2D(this Vector3 v, Vector3 v2) => Vector2.Distance(v.ToVector2(), v2.ToVector2());

    internal static bool ContainsIgnoreCase(this string haystack, string needle)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
    }

    public static uint SetAlpha(this uint color32, uint alpha)
    {
        return (color32 << 8 >> 8) + (alpha << 24);
    }

    public static uint Invert(this uint color32)
    {
        return (uint.MaxValue - (color32 << 8) >> 8) + (color32 >> 24 << 24);
    }

    public static Vector2 Normalize(this Vector2 v)
    {
        return Vector2.Normalize(v);
    }

    public static Vector2 Zoom(this Vector2 vin, float zoom, Vector2 origin = default)
    {
        return origin + (vin - origin) * zoom;
    }

    public static Vector2 Rotate(this Vector2 vin, float rad, Vector2 pivot = default)
    {
        var rotation = rad.ToNormalizedVector2();
        var diff = vin - pivot;
        return pivot + new Vector2(rotation.Y * diff.X - rotation.X * diff.Y, rotation.Y * diff.Y + rotation.X * diff.X);
    }

    public static Vector2 Rotate(this Vector2 vin, Vector2 rotation, Vector2 pivot = default)
    {
        rotation = rotation.Normalize();
        var diff = vin - pivot;
        return pivot + new Vector2(rotation.Y * diff.X - rotation.X * diff.Y, rotation.Y * diff.Y + rotation.X * diff.X);
    }

    public static float ToArc(this Vector2 vin) => MathF.Sin(vin.X);

    public static void MassTranspose(Vector2[] vin, Vector2 rotation, Vector2 pivot = default)
    {
        for (int i = 0; i < vin.Length; i++)
        {
            vin[i] = vin[i].Rotate(rotation, pivot);
        }
    }

    public static void MassTranspose(Vector2[] vin, float rotation, Vector2 pivot = default)
    {
        for (int i = 0; i < vin.Length; i++)
        {
            vin[i] = vin[i].Rotate(rotation, pivot);
        }
    }

    public static Vector2 ToNormalizedVector2(this float rad) => new(MathF.Sin(rad), MathF.Cos(rad));


    public static string GetRelative(this nint i)
    {
        return (i.ToInt64() - Service.SigScanner.Module.BaseAddress.ToInt64()).ToString("X");
    }

    public static string ToCompressedString<T>(this T obj)
    {
        return Compress(obj.ToJsonString());
    }

    public static T? DecompressStringToObject<T>(this string compressedString)
    {
        return Decompress(compressedString).JsonStringToObject<T>();
    }

    public static string ToJsonString(this object? obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public static T? JsonStringToObject<T>(this string str)
    {
        return JsonConvert.DeserializeObject<T>(str);
    }

    public static byte[] GetSHA1(string s)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(s);
        return SHA1.HashData(bytes);
    }

    public static string Compress(string s)
    {
        string result;
        using (MemoryStream memoryStream2 = new(Encoding.Unicode.GetBytes(s)))
        {
            using MemoryStream memoryStream3 = new();
            using (GZipStream destination = new(memoryStream3, CompressionLevel.Optimal))
            {
                memoryStream2.CopyTo(destination);
            }
            result = Convert.ToBase64String(memoryStream3.ToArray());
        }
        return result;
    }

    public static string Decompress(string s)
    {
        string @string;
        using (MemoryStream stream = new(Convert.FromBase64String(s)))
        {
            using MemoryStream memoryStream = new();
            using (GZipStream gZipStream = new(stream, CompressionMode.Decompress))
            {
                gZipStream.CopyTo(memoryStream);
            }
            @string = Encoding.Unicode.GetString(memoryStream.ToArray());
        }
        return @string;
    }
    public static Direction GetDirection(Vector2 origin, Vector3 target)
    {
        var diff = new Vector2(target.X - origin.X, target.Z - origin.Y);
        var valueRadian = MathF.Atan2(diff.Y, diff.X);
        if (-112.5f / 180f * MathF.PI <= valueRadian && valueRadian <= -67.5f / 180f * MathF.PI)
            return Direction.正北;
        if (-67.5f / 180f * MathF.PI <= valueRadian && valueRadian <= -22.5f / 180f * MathF.PI)
            return Direction.东北;
        if (-22.5f / 180f * MathF.PI <= valueRadian && valueRadian <= 22.5f / 180f * MathF.PI)
            return Direction.正东;
        if (22.5f / 180f * MathF.PI <= valueRadian && valueRadian <= 67.5f / 180f * MathF.PI)
            return Direction.东南;
        if (67.5f / 180f * MathF.PI <= valueRadian && valueRadian <= 112.5f / 180f * MathF.PI)
            return Direction.正南;
        if (112.5f / 180f * MathF.PI <= valueRadian && valueRadian <= 157.5f / 180f * MathF.PI)
            return Direction.西南;
        if (157.5f / 180f * MathF.PI <= valueRadian || valueRadian <= -157.5f / 180f * MathF.PI)
            return Direction.正西;
        if (-157.5f / 180f * MathF.PI <= valueRadian && valueRadian <= -112.5f / 180f * MathF.PI)
            return Direction.西北;
        return Direction.None;
    }

    public static Vector2[] GenerateBentLineWithWidth(Vector2 start, Vector2 end, float halfWidth, bool horizontal)
    {
        var tolerance = 2f;
        Vector2 endpointDirection = horizontal ? new(0, -1) : new(1, 0);
        var endpointOffset = halfWidth * endpointDirection;
        var edge1start = start + endpointOffset;
        var edge1end = end + endpointOffset;
        var edge2start = start - endpointOffset;
        var edge2end = end - endpointOffset;
        if (horizontal && (MathF.Abs(start.Y - end.Y) < tolerance || MathF.Abs(start.X - end.X) <= 2 * halfWidth)
            || !horizontal && (MathF.Abs(start.X - end.X) < tolerance || MathF.Abs(start.Y - end.Y) <= 2 * halfWidth))
        {
            return [edge1start, edge1end, edge2end, edge2start];
        }

        Vector2 firstBend, secondBend, bendDirection, bendOffset;
        if (horizontal)
        {
            firstBend = new((start.X + end.X) / 2, start.Y);  // 第一个拐点
            secondBend = new((start.X + end.X) / 2, end.Y);   // 第二个拐点
            bendDirection = new(start.Y < end.Y ? 1 : -1, -1);
        }
        else
        {
            firstBend = new(start.X, (start.Y + end.Y) / 2);  // 第一个拐点
            secondBend = new(end.X, (start.Y + end.Y) / 2);   // 第二个拐点
            bendDirection = new(1, start.X < end.X ? -1 : 1);
        }
        bendOffset = halfWidth * bendDirection;
        return [edge1start,
            firstBend + bendOffset,
            secondBend + bendOffset,
            edge1end, 
            edge2end,
            secondBend - bendOffset,
            firstBend - bendOffset,
            edge2start];
    }
}
