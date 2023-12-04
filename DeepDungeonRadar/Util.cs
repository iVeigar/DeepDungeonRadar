using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Services;
using ImGuiScene;
using Newtonsoft.Json;

namespace DeepDungeonRadar;

internal static class Util
{
    public static Vector2 GetSize(this TextureWrap textureWrap)
    {
        return new Vector2(textureWrap.Width, textureWrap.Height);
    }

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
        return pivot + new Vector2(rotation.Y * diff.X + rotation.X * diff.Y, rotation.Y * diff.Y - rotation.X * diff.X);
    }

    public static Vector2 Rotate(this Vector2 vin, Vector2 rotation, Vector2 pivot = default)
    {
        rotation = rotation.Normalize();
        var diff = vin - pivot;
        return pivot + new Vector2(rotation.Y * diff.X + rotation.X * diff.Y, rotation.Y * diff.Y - rotation.X * diff.X);
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
        return (i.ToInt64() - PluginService.SigScanner.Module.BaseAddress.ToInt64()).ToString("X");
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

    public static DeepDungeonBg TerritoryToBg(ushort territory)
    {
        return territory switch
        {
            561 => DeepDungeonBg.f1c1,
            562 => DeepDungeonBg.f1c2,
            563 => DeepDungeonBg.f1c3,
            564 or 565 => DeepDungeonBg.f1c4,
            593 or 594 or 595 => DeepDungeonBg.f1c5,
            596 or 597 or 598 => DeepDungeonBg.f1c6,
            599 or 600 => DeepDungeonBg.f1c8,
            601 or 602 => DeepDungeonBg.f1c9,
            603 or 604 or 605 or 606 or 607 => DeepDungeonBg.f1c7,
            770 => DeepDungeonBg.e3c1,
            771 => DeepDungeonBg.e3c2,
            772 or 782 => DeepDungeonBg.e3c3,
            773 or 783 => DeepDungeonBg.e3c4,
            774 or 784 => DeepDungeonBg.e3c5,
            775 or 785 => DeepDungeonBg.e3c6,
            1099 => DeepDungeonBg.l5c1,
            1100 => DeepDungeonBg.l5c2,
            1101 or 1102 => DeepDungeonBg.l5c3,
            1103 or 1104 => DeepDungeonBg.l5c4,
            1105 or 1106 => DeepDungeonBg.l5c5,
            1107 or 1108 => DeepDungeonBg.l5c6,
            _ => DeepDungeonBg.notInKnownDeepDungeon,
        };
    }
}
