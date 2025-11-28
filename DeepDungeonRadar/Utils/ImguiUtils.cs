using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace DeepDungeonRadar.Utils;

internal static class ImguiUtils
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

    public static void DrawDotWithText(this ImDrawListPtr drawList, Vector2 pos, string str, uint color, uint strokeColor)
    {
        drawList.DrawText(pos, str, color, true, true, strokeColor);
        drawList.AddCircleFilled(pos, Plugin.Config.MarkerDotSize, color);
        drawList.AddCircle(pos, Plugin.Config.MarkerDotSize, strokeColor, 0, 1);
    }
}
