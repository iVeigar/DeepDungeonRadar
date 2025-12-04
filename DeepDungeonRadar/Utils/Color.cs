namespace DeepDungeonRadar.Utils;

public static class Color // ImGui ABGR
{
    public static uint ToRgba(this uint color)
    {
        return ((color & 0xff) << 24)
            | ((color & 0xff00) << 8)
            | ((color & 0xff0000) >> 8)
            | ((color & 0xff000000) >> 24);
    }

    public static uint ToArgb(this uint color)
    {
        return (color & 0xff000000)
            | ((color & 0xff) << 16)
            | (color & 0xff00)
            | ((color & 0xff0000) >> 16);
    }

    public const uint Red = 0xFF0000FF;

    public const uint Magenta = 0xFFFF00FF;

    public const uint Yellow = 0xFF00FFFF;

    public const uint Green = 0xFF00FF00;

    public const uint LightGreen = 0xFF90EE90;

    public const uint GrassGreen = 0xFF00E000;

    public const uint Cyan = 0xFFFFFF00;

    public const uint DarkCyan = 0xFF909000;

    public const uint LightCyan = 0xFFFFFFA0;

    public const uint Blue = 0xFFFF0000;

    public const uint LightBlue = 0xFFE6D8AD;

    public const uint Black = 0xFF000000;

    public const uint TransBlack = 0x80000000;

    public const uint Grey = 0xFF808080;

    public const uint TransGrey = 0x80808080;

    public const uint White = 0xFFFFFFFF;

    public const uint Gold = 0xFF00D7FF;

    public const uint Chocolate = 0xFF1D66CD;
}
