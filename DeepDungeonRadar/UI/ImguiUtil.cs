using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using DeepDungeonRadar.Services;
using ImGuiNET;
using ImGuiScene;

namespace DeepDungeonRadar.UI;

internal static class ImguiUtil
{
    private const int CircleSegments = 50;
    private const float CircleSegmentFullRotation = 2 * MathF.PI / CircleSegments;

    public static void ColorPickerWithPalette(int id, string description, ref Vector4 originalColor, ImGuiColorEditFlags flags)
    {
        Vector4 col = originalColor;
        List<Vector4> list = ImGuiHelpers.DefaultColorPalette(36);
        if (ImGui.ColorButton($"{description}###ColorPickerButton{id}", originalColor, flags))
        {
            ImGui.OpenPopup($"###ColorPickerPopup{id}");
        }

        if (!ImGui.BeginPopup($"###ColorPickerPopup{id}"))
        {
            return;
        }

        if (ImGui.ColorPicker4($"###ColorPicker{id}", ref col, flags))
        {
            originalColor = col;
        }
        for (int i = 0; i < 4; i++)
        {
            ImGui.Spacing();
            for (int j = i * 9; j < i * 9 + 9; j++)
            {
                if (ImGui.ColorButton($"###ColorPickerSwatch{id}{i}{j}", list[j]))
                {
                    originalColor = list[j];
                    ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                    return;
                }
                ImGui.SameLine();
            }
        }
        ImGui.EndPopup();
    }

    private static void DrawCircleInternal(this ImDrawListPtr drawList, Vector3 center, float radius, float thickness, uint color, bool filled)
    {
        for (var i = 0; i <= CircleSegments; i++)
        {
            var currentRotation = i * CircleSegmentFullRotation;
            var segmentWorld = center + (radius * currentRotation.ToNormalizedVector2()).ToVector3();
            PluginService.GameGui.WorldToScreen(segmentWorld, out var segmentScreen);
            drawList.PathLineTo(segmentScreen);
        }

        if (filled)
            drawList.PathFillConvex(color);
        else
            drawList.PathStroke(color, ImDrawFlags.RoundCornersDefault, thickness);
    }

    public static bool DrawRingWorld(this ImDrawListPtr drawList, Vector3 center, float radius, float thickness, uint color)
    {
        PluginService.GameGui.WorldToScreen(center, out var _, out var inView);
        if (inView)
        {
            drawList.DrawCircleInternal(center, radius, thickness, color, false);
        }
        return inView;
    }

    public static bool DrawRingWorldWithText(this ImDrawListPtr drawList, Vector3 center, float radius, float thickness, uint color, string text, Vector2 offset = default)
    {
        PluginService.GameGui.WorldToScreen(center, out var screenPos, out var inView);
        if (inView)
        {
            drawList.DrawCircleInternal(center, radius, thickness, color, false);
            drawList.DrawTextWithBorderBg(screenPos + offset, text, color, Color.TransBlack, true);
        }
        return inView;
    }

    internal static bool IconButton(FontAwesomeIcon icon, string id, Vector2 size)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool result = ImGui.Button(icon.ToIconString() + "##" + id, size);
        ImGui.PopFont();
        return result;
    }

    internal static bool IconButton(FontAwesomeIcon icon, string id)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool result = ImGui.Button(icon.ToIconString() + "##" + id);
        ImGui.PopFont();
        return result;
    }

    internal static bool ComboEnum<T>(this T eEnum, string label) where T : Enum
    {
        bool result = false;
        Type typeFromHandle = typeof(T);
        string[] names = Enum.GetNames(typeFromHandle);
        Array values = Enum.GetValues(typeFromHandle);
        ImGui.BeginCombo(label, eEnum.ToString());
        for (int i = 0; i < names.Length; i++)
        {
            if (ImGui.Selectable(names[i] + "##" + label))
            {
                eEnum = (T)values.GetValue(i);
                result = true;
            }
        }
        ImGui.EndCombo();
        return result;
    }

    public static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint col, bool stroke, bool centerAlignX = true, uint strokecol = 4278190080U)
    {
        if (centerAlignX)
        {
            pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
        }
        if (stroke)
        {
            drawList.AddText(pos + new Vector2(-1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(-1f, 1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, -1f), strokecol, text);
            drawList.AddText(pos + new Vector2(1f, 1f), strokecol, text);
        }
        drawList.AddText(pos, col, text);
    }

    public static void DrawTextWithBg(this ImDrawListPtr drawList, Vector2 pos, string text, uint col = 4294967295U, uint bgcol = 4278190080U, bool centerAlignX = true)
    {
        Vector2 vector = ImGui.CalcTextSize(text) + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f);
        if (centerAlignX)
        {
            pos -= new Vector2(vector.X, 0f) / 2f;
        }
        drawList.AddRectFilled(pos, pos + vector, bgcol);
        drawList.AddText(pos + new Vector2(ImGui.GetStyle().ItemSpacing.X / 2f, 0f), col, text);
    }

    public static void DrawTextWithBorderBg(this ImDrawListPtr drawList, Vector2 pos, string text, uint col = 4294967295U, uint bgcol = 4278190080U, bool centerAlignX = true)
    {
        Vector2 vector = ImGui.CalcTextSize(text) + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f);
        if (centerAlignX)
        {
            pos -= new Vector2(vector.X, 0f) / 2f;
        }
        drawList.AddRectFilled(pos, pos + vector, bgcol, 10f); // 圆角
        drawList.AddRect(pos, pos + vector, col, 10f);
        drawList.AddText(pos + new Vector2(ImGui.GetStyle().ItemSpacing.X / 2f + 0.5f, -0.5f), col, text);
    }

    public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, uint bgcolor, float rotation, float thickness, float outlinethickness)
    {
        drawList.AddPolyline(ref (new Vector2[]
        {
            pos + new Vector2(0f - size - outlinethickness / 2f, -0.5f * size - outlinethickness / 2f).Rotate(rotation),
            pos + new Vector2(0f, 0.5f * size).Rotate(rotation),
            pos + new Vector2(size + outlinethickness / 2f, -0.5f * size - outlinethickness / 2f).Rotate(rotation)
        })[0], 3, bgcolor, ImDrawFlags.RoundCornersAll, thickness + outlinethickness);
        drawList.DrawArrow(pos, size, color, rotation, thickness);
    }

    public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, float rotation, float thickness)
    {
        drawList.AddPolyline(ref (new Vector2[]
        {
            pos + new Vector2(0f - size, -0.5f * size).Rotate(rotation),
            pos + new Vector2(0f, 0.5f * size).Rotate(rotation),
            pos + new Vector2(size, -0.5f * size).Rotate(rotation)
        })[0], 3, color, ImDrawFlags.RoundCornersAll, thickness);
    }

    public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, uint bgcolor, Vector2 rotation, float thickness, float outlinethickness)
    {
        drawList.AddPolyline(ref (new Vector2[]
        {
            pos + new Vector2(0f - size - outlinethickness / 2f, -0.4f * size - outlinethickness / 2f).Rotate(rotation),
            pos + new Vector2(0f, 0.6f * size).Rotate(rotation),
            pos + new Vector2(size + outlinethickness / 2f, -0.4f * size - outlinethickness / 2f).Rotate(rotation)
        })[0], 3, bgcolor, ImDrawFlags.RoundCornersAll, thickness + outlinethickness);
        drawList.DrawArrow(pos, size, color, rotation, thickness);
    }

    public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, Vector2 rotation, float thickness)
    {
        drawList.AddPolyline(ref (new Vector2[]
        {
            pos + new Vector2(0f - size, -0.4f * size).Rotate(rotation),
            pos + new Vector2(0f, 0.6f * size).Rotate(rotation),
            pos + new Vector2(size, -0.4f * size).Rotate(rotation)
        })[0], 3, color, ImDrawFlags.RoundCornersAll, thickness);
    }

    public static void DrawTrangle(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, Vector2 rotation, bool filled = true)
    {
        Vector2[] array = GettriV(pos, size, rotation);
        if (filled)
        {
            drawList.AddTriangleFilled(array[0], array[1], array[2], color);
            return;
        }
        drawList.AddTriangle(array[0], array[1], array[2], color);

        static Vector2[] GettriV(Vector2 vin, float s, Vector2 rotation)
        {
            rotation = rotation.Normalize();
            Vector2 vin2 = new(0f, s * 1.7320508f - s * (2f / 3f));
            Vector2 vin3 = new((0f - s) * 0.8f, (0f - s) * (2f / 3f));
            Vector2 vin4 = new(s * 0.8f, (0f - s) * (2f / 3f));
            vin2 = vin + vin2.Rotate(rotation);
            vin3 = vin + vin3.Rotate(rotation);
            vin4 = vin + vin4.Rotate(rotation);
            return new Vector2[3] { vin2, vin3, vin4 };
        }
    }

    public static void DrawMapTextDot(this ImDrawListPtr drawList, Vector2 pos, string? str, uint fgcolor, uint bgcolor)
    {
        if (!string.IsNullOrWhiteSpace(str))
        {
            drawList.DrawText(pos, str, fgcolor, PluginService.Config.RadarTextStroke, true, bgcolor);
        }
        drawList.AddCircleFilled(pos, PluginService.Config.RadarObjectDotSize, fgcolor);
        if (PluginService.Config.RadarObjectDotStroke != 0f)
        {
            drawList.AddCircle(pos, PluginService.Config.RadarObjectDotSize, bgcolor, 0, PluginService.Config.RadarObjectDotStroke);
        }
    }

    public static void DrawIcon(this ImDrawListPtr drawlist, Vector2 pos, TextureWrap icon, float size = 1f)
    {
        _ = icon.GetSize() * size;
        drawlist.AddImage(icon.ImGuiHandle, pos, pos);
    }

    public static Vector2 ToRadarWindowPos(this Vector3 vin, Vector3 pivotWorldPos, Vector2 pivotWindowPos, float zoom, float rotation)
    {
        return pivotWindowPos + (vin - pivotWorldPos).ToVector2().Zoom(zoom).Rotate(rotation);
    }

    public static Vector2 ToRadarWindowPos(this Vector2 vin, Vector2 pivotWorldPos, Vector2 pivotWindowPos, float zoom, float rotation)
    {
        return pivotWindowPos + (vin - pivotWorldPos).Zoom(zoom).Rotate(rotation);
    }
}
