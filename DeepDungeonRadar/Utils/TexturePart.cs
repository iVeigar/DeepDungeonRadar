using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;

namespace DeepDungeonRadar.Utils;

public class TexturePart
{
    public uint IconId { get; init; }
    public string Path { get; init; }
    public Vector2 Position { get; init; }
    public Vector2 Size { get; init; }
    public Vector2 Origin { get; init; }
    public TexturePart(string path, Vector2 position, Vector2 size, Vector2 origin = default)
    {
        Path = path;
        Position = position;
        Size = size;
        Origin = origin;
    }
    public TexturePart(uint iconId)
    {
        IconId = iconId;
    }
    public void DrawOnWindow(ImDrawListPtr drawListPtr, Vector2 target, Vector2 scale, float rotation = 0, uint color = Color.White)
    {
        if (IconId != 0)
        {
            drawListPtr.DrawIcon(IconId, target, scale, rotation, color);
        }
        else
        {
            drawListPtr.DrawTexturePart(Path, Position, Size, Origin, target, scale, rotation, color);
        }
    }
    public void DrawOnWindow(ImDrawListPtr drawListPtr, Vector2 target, float scale = 1, float rotation = 0, uint color = Color.White)
        => DrawOnWindow(drawListPtr, target, new Vector2(scale), rotation, color);
}

public static partial class ImGuiUtils
{
    public static bool DrawTexturePart(this ImDrawListPtr drawListPtr, string path, Vector2 position, Vector2 size, Vector2 origin, Vector2 target, float scale = 1, float rotation = 0, uint color = Color.White)
        => DrawTexturePart(drawListPtr, path, position, size, origin, target, new Vector2(scale), rotation, color);

    public static bool DrawTexturePart(this ImDrawListPtr drawListPtr, string path, Vector2 position, Vector2 size, Vector2 origin, Vector2 target, Vector2 scale, float rotation = 0, uint color = Color.White)
    {
        if (!ThreadLoadImageHandler.TryGetTextureWrap(path, out var tex))
            return false;

        var mat = BuildTransformMatrix(origin, target, scale, rotation);
        var lt = Vector2.Zero.Transform(mat);
        var rt = new Vector2(size.X, 0).Transform(mat);
        var rb = size.Transform(mat);
        var lb = new Vector2(0, size.Y).Transform(mat);

        var uv1 = position / tex.Size;
        var uv2 = new Vector2(position.X + size.X, position.Y) / tex.Size;
        var uv3 = (position + size) / tex.Size;
        var uv4 = new Vector2(position.X, position.Y + size.Y) / tex.Size;

        drawListPtr.AddImageQuad(tex.Handle,
            lt, rt, rb, lb,
            uv1, uv2, uv3, uv4,
            color);
        return true;
    }

    public static bool DrawIcon(this ImDrawListPtr drawListPtr, uint iconId, Vector2 target, float scale = 1, float rotation = 0, uint color = Color.White)
        => DrawIcon(drawListPtr, iconId, target, new Vector2(scale), rotation, color);

    public static bool DrawIcon(this ImDrawListPtr drawListPtr, uint iconId, Vector2 target, Vector2 scale, float rotation = 0, uint color = Color.White)
    {
        if (!ThreadLoadImageHandler.TryGetIconTextureWrap(iconId, true, out var tex))
            return false;

        var mat = BuildTransformMatrix(tex.Size / 2, target, scale, rotation);
        var lt = Vector2.Zero.Transform(mat);
        var rt = new Vector2(tex.Width, 0).Transform(mat);
        var rb = tex.Size.Transform(mat);
        var lb = new Vector2(0, tex.Height).Transform(mat);
        drawListPtr.AddImageQuad(tex.Handle,
            lt, rt, rb, lb,
            color);
        return true;
    }
}
