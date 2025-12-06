using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace DeepDungeonRadar.Utils;

public static partial class ImGuiUtils
{
    public static bool ColorPickerWithPalette(int id, string label, ref uint originalColor, ImGuiColorEditFlags flags = ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview)
    {
        var changed = false;
        Vector4 color = ImGui.ColorConvertU32ToFloat4(originalColor);
        if (ImGui.ColorButton($"{label}###ColorPickerButton{id}", color, flags))
        {
            ImGui.OpenPopup($"###ColorPickerPopup{id}");
        }

        if (!ImGui.BeginPopup($"###ColorPickerPopup{id}"))
        {
            return false;
        }

        changed |= ImGui.ColorPicker4($"###ColorPicker{id}", ref color, flags);

        List<Vector4> list = ImGuiHelpers.DefaultColorPalette();
        for (int i = 0; i < 4; i++)
        {
            ImGui.Spacing();
            for (int j = 0; j < 8; j++)
            {
                var index = i * 8 + j;
                if (ImGui.ColorButton($"###ColorPaletteButton{id}-{index}", list[index]))
                {
                    color = list[index];
                    changed = true;
                }
                ImGui.SameLine();
            }
        }
        ImGui.EndPopup();
        if (changed)
        {
            originalColor = ImGui.ColorConvertFloat4ToU32(color);
        }
        return changed;
    }

    internal static bool IconButton(FontAwesomeIcon icon, string id, Vector2 size = default)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool result = ImGui.Button(icon.ToIconString() + "##" + id, size);
        ImGui.PopFont();
        return result;
    }

    public static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint color, bool stroke, bool centerAlignX = true, uint strokecolor = Color.Black)
    {
        if (centerAlignX)
        {
            pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
        }
        if (stroke)
        {
            drawList.AddText(pos + new Vector2(-1f, -1f), strokecolor, text);
            drawList.AddText(pos + new Vector2(-1f, 1f), strokecolor, text);
            drawList.AddText(pos + new Vector2(1f, -1f), strokecolor, text);
            drawList.AddText(pos + new Vector2(1f, 1f), strokecolor, text);
        }
        drawList.AddText(pos, color, text);
    }

    public static void DrawDotWithText(this ImDrawListPtr drawList, Vector2 pos, string str, uint color, uint strokeColor = Color.Black)
    {
        drawList.DrawText(pos, str, color, true, true, strokeColor);
        drawList.AddCircleFilled(pos, Plugin.Config.MarkerDotSize, color);
        drawList.AddCircle(pos, Plugin.Config.MarkerDotSize, strokeColor, 0, 1);
    }

    public static Matrix3x2 BuildTransformMatrix(Vector2 pivot, Vector2 position, Vector2 scale, float rot)
    {
        return Matrix3x2.CreateTranslation(-pivot)
          * Matrix3x2.CreateScale(scale)
          * Matrix3x2.CreateRotation(-rot) // yes -rot
          * Matrix3x2.CreateTranslation(position);
    }
    public static Matrix3x2 BuildTransformMatrix(Vector2 pivot, Vector2 position, float scale, float rot)
    {
        return Matrix3x2.CreateTranslation(-pivot)
          * Matrix3x2.CreateScale(scale)
          * Matrix3x2.CreateRotation(-rot) // yes -rot
          * Matrix3x2.CreateTranslation(position);
    }
}
