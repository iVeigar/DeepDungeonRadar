using System;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.UI;
using Dalamud.Bindings.ImGui;

namespace DeepDungeonRadar.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;

    public ConfigWindow() : base("Deep Dungeon Radar Config", ImGuiWindowFlags.None)
    {
        Size = new(480f, 640f);
        SizeCondition = ImGuiCond.FirstUseEver;
        config = Service.Config;
    }

    public void Dispose()
    {

    }

    public override void Draw()
    {
        var radarEnabled = config.RadarEnabled;
        if (ImGui.Checkbox("启用##RadarEnabled", ref radarEnabled))
        {
            config.RadarEnabled = radarEnabled;
            config.Save();
        }

        var radarShowCenter = config.RadarShowCenter;
        if (ImGui.Checkbox("显示自己##RadarShowCenter", ref radarShowCenter))
        {
            config.RadarShowCenter = radarShowCenter;
            config.Save();
        }

        var radarShowAssistCircle = config.RadarShowAssistCircle;
        if (ImGui.Checkbox("显示自己时显示辅助圈(25m|125m)##RadarShowAssistCircle", ref radarShowAssistCircle))
        {
            config.RadarShowAssistCircle = radarShowAssistCircle;
            config.Save();
        }

        var radarAlwaysFacingNorth = config.RadarOrientationFixed;
        if (ImGui.Checkbox("固定指北##RadarAlwaysFacingNorth", ref radarAlwaysFacingNorth))
        {
            config.RadarOrientationFixed = radarAlwaysFacingNorth;
            config.Save();
        }

        var radarClickThrough = config.RadarClickThrough;
        if (ImGui.Checkbox("鼠标穿透##RadarClickThrough", ref radarClickThrough))
        {
            config.RadarClickThrough = radarClickThrough;
            config.Save();
        }

        var radarLockSizePos = config.RadarLockSizePos;
        if (ImGui.Checkbox("锁定窗口位置和尺寸##RadarLockSizePos", ref radarLockSizePos))
        {
            config.RadarLockSizePos = radarLockSizePos;
            config.Save();
        }
        var radarDrawStraightCorridor = config.RadarDrawStraightCorridor;
        if (ImGui.Checkbox("画直线型过道##radarDrawStraightCorridor", ref radarDrawStraightCorridor))
        {
            config.RadarDrawStraightCorridor = radarDrawStraightCorridor;
            config.Save();
        }
        
        if (ImGui.CollapsingHeader("地图颜色"))
        {
            ImGui.Text("不可达区域边界"); ImGui.SameLine();
            var radarMapColor1 = ImGui.ColorConvertU32ToFloat4(config.RadarMapInactiveForegroundColor);
            ImguiUtil.ColorPickerWithPalette(1, string.Empty, ref radarMapColor1, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
            uint color1 = ImGui.ColorConvertFloat4ToU32(radarMapColor1);
            if (color1 != config.RadarMapInactiveForegroundColor)
            {
                config.RadarMapInactiveForegroundColor = color1;
                config.Save();
            }

            ImGui.Text("不可达区域背景"); ImGui.SameLine();
            var radarMapColor2 = ImGui.ColorConvertU32ToFloat4(config.RadarMapInactiveBackgroundColor);
            ImguiUtil.ColorPickerWithPalette(2, string.Empty, ref radarMapColor2, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
            uint color2 = ImGui.ColorConvertFloat4ToU32(radarMapColor2);
            if (color2 != config.RadarMapInactiveBackgroundColor)
            {
                config.RadarMapInactiveBackgroundColor = color2;
                config.Save();
            }

            ImGui.Text("可达区域边界"); ImGui.SameLine();
            var radarMapColor3 = ImGui.ColorConvertU32ToFloat4(config.RadarMapActiveForegroundColor);
            ImguiUtil.ColorPickerWithPalette(3, string.Empty, ref radarMapColor3, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
            uint color3 = ImGui.ColorConvertFloat4ToU32(radarMapColor3);
            if (color3 != config.RadarMapActiveForegroundColor)
            {
                config.RadarMapActiveForegroundColor = color3;
                config.Save();
            }

            ImGui.Text("可达区域背景"); ImGui.SameLine();
            var radarMapColor4 = ImGui.ColorConvertU32ToFloat4(config.RadarMapActiveBackgroundColor);
            ImguiUtil.ColorPickerWithPalette(4, string.Empty, ref radarMapColor4, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
            uint color4 = ImGui.ColorConvertFloat4ToU32(radarMapColor4);
            if (color4 != config.RadarMapActiveBackgroundColor)
            {
                config.RadarMapActiveBackgroundColor = color4;
                config.Save();
            }
        }


        ImGui.TextUnformatted("标记设置");

        var radarObjectDotSize = config.RadarObjectDotSize;
        if (ImGui.SliderFloat("标记点大小##RadarObjectDotSize", ref radarObjectDotSize, 3f, 15f))
        {
            config.RadarObjectDotSize = radarObjectDotSize;
            config.Save();
        }

        var radarObjectDotStroke = config.RadarObjectDotStroke;
        if (ImGui.SliderFloat("标记点描边宽度##RadarObjectDotStroke", ref radarObjectDotStroke, 0f, 5f))
        {
            config.RadarObjectDotStroke = radarObjectDotStroke;
            config.Save();
        }

        var radarDetailLevel = (int)config.RadarDetailLevel;
        if (ImGui.SliderInt("信息显示级别##RadarDetailLevel", ref radarDetailLevel, 0, 2, ((DetailLevel)radarDetailLevel).ToString()))
        {
            config.RadarDetailLevel = (DetailLevel)radarDetailLevel;
            config.Save();
        }

        if (ImGui.GetIO().Fonts.Fonts.Size > 2)
        {
            var radarUseLargeFont = config.RadarUseLargeFont;
            if (ImGui.Checkbox("大字体##RadarUseLargeFont", ref radarUseLargeFont))
            {
                config.RadarUseLargeFont = radarUseLargeFont;
                config.Save();
            }
        }

        var radarTextStroke = config.RadarTextStroke;
        if (ImGui.Checkbox("文字描边##RadarTextStroke", ref radarTextStroke))
        {
            config.RadarTextStroke = radarTextStroke;
            config.Save();
        }
    }
}
