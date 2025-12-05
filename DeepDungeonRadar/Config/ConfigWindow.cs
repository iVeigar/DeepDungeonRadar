using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Utils;
using PunishLib.ImGuiMethods;

namespace DeepDungeonRadar.Config;

public sealed class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly Plugin plugin;
    private bool generalSettingsChanged = false;
    private bool drawingSettingsChanged = false;
    public ConfigWindow(Plugin plugin) : base("深宫小地图 - 设置", ImGuiWindowFlags.None)
    {
        this.plugin = plugin;
        config = Plugin.Config;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(400, 500), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("settingsTabBar");
        if (!tabBar) return;

        DrawTabGeneralSettings();
        DrawTabDrawingSettings();
    }

    public override void OnClose()
    {
        if (generalSettingsChanged || drawingSettingsChanged)
        {
            config.Save();
            generalSettingsChanged = drawingSettingsChanged = false;
        }
    }

    private void DrawTabGeneralSettings()
    {
        using var tabItem = ImRaii.TabItem("一般");
        if (!tabItem) return;

        if (ImGui.Checkbox("启用小地图", ref config.RadarEnabled))
        {
            plugin.ToggleRadar(config.RadarEnabled);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        generalSettingsChanged |= ImGui.Checkbox("固定指北", ref config.RadarOrientationFixed);
        generalSettingsChanged |= ImGui.Checkbox("鼠标穿透", ref config.RadarClickThrough);
        if (!config.RadarClickThrough)
        {
            ImGui.Indent();
            generalSettingsChanged |= ImGui.Checkbox("锁定窗口位置和尺寸##RadarLockSizePos", ref config.RadarLockSizePos);
            ImGui.Unindent();
        }
        generalSettingsChanged |= ImGui.Checkbox("绘制不可达区域", ref config.RadarDrawUnreachable);
        generalSettingsChanged |= ImGui.Checkbox("显示碰撞盒标记点", ref config.ShowColliderBoxDot);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("房间出口地面上的绿/红圆点，表示该处碰撞盒可穿行/不可穿行");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGuiGroup.BeginGroupBox("小地图标记"))
        {

            generalSettingsChanged |= ImGui.SliderFloat("传送门方位箭头大小##RadarPassageArrowScale", ref config.RadarPassageArrowScale, 0.1f, 10f);
            ImGui.Spacing();
            generalSettingsChanged |= ImGui.SliderFloat("实体标记点大小##RadarObjectDotSize", ref config.MarkerDotSize, 2f, 15f);
            ImGui.Spacing();
            generalSettingsChanged |= ImGui.Checkbox("移除实体名称前缀", ref config.RemoveNamePrefix);
            ImGui.Text("名称前缀");
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("每行一个前缀");
            generalSettingsChanged |= ImGui.InputTextMultiline(string.Empty, ref config.NamePrefixes, 512, default);

            ImGuiGroup.EndGroupBox();
        }
    }

    private void DrawTabDrawingSettings()
    {
        using var tabItem = ImRaii.TabItem("绘图");
        if (!tabItem) return;

        drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(0, "辅助圈颜色", ref config.RadarSenseCircleOutlineColor);
        ImGui.SameLine();
        ImGui.Text("辅助圈");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("半径80m，游戏对象基本上会在进入范围前就显示在雷达上\n可以用这个特性快速寻宝、找烛台");
        
        ImGui.Separator();

        if (ImGuiGroup.BeginGroupBox("地图颜色"))
        {
            using var id = ImRaii.PushId("地图颜色");
            drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(1, "可达区域边界", ref config.ReachableAreaBorderColor);
            ImGui.SameLine();
            ImGui.Text("可达区域边界"); 

            drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(2, "可达区域背景", ref config.ReachableAreaBackgroundColor);
            ImGui.SameLine();
            ImGui.Text("可达区域背景");  

            drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(3, "不可达区域边界", ref config.UnreachableAreaBorderColor);
            ImGui.SameLine();
            ImGui.Text("不可达区域边界"); 

            drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(4, "不可达区域背景", ref config.UnreachableAreaBackgroundColor);
            ImGui.SameLine();
            ImGui.Text("不可达区域背景");

            drawingSettingsChanged |= ImguiUtils.ColorPickerWithPalette(5, "地图窗口背景", ref config.RadarWindowBgColor);
            ImGui.SameLine();
            ImGui.Text("地图窗口背景");
            ImGuiGroup.EndGroupBox();
        }
        ImGui.Spacing();
        ImGui.BulletText("优先级更高的标记会绘制在图层更上层\n本地玩家的标记始终置顶");
        ImGui.Spacing();
        drawingSettingsChanged |= DrawMarkerConfig("玩家", "玩家", ref config.Markers.Player);
        drawingSettingsChanged |= DrawMarkerConfig("怪物", "怪物", ref config.Markers.Enemy);
        drawingSettingsChanged |= DrawMarkerConfig("友好生物和npc", "友好生物", ref config.Markers.Friendly);
        drawingSettingsChanged |= DrawMarkerConfig("装置（传送、再生、烛台）", "装置", ref config.Markers.EventObj);
        drawingSettingsChanged |= DrawMarkerConfig("金宝箱", "金宝箱", ref config.Markers.GoldChest);
        drawingSettingsChanged |= DrawMarkerConfig("银宝箱", "银宝箱", ref config.Markers.SilverChest);
        drawingSettingsChanged |= DrawMarkerConfig("铜宝箱", "铜宝箱", ref config.Markers.BronzeChest);
        drawingSettingsChanged |= DrawMarkerConfig("埋藏的宝藏", "宝藏", ref config.Markers.AccursedHoard);
    }

    private static bool DrawMarkerConfig(string title, string id, ref Marker marker)
    {
        var changed = false;
        using var a = ImRaii.PushId(id);
        if (ImGuiGroup.BeginGroupBox(title))
        {
            changed |= ImguiUtils.ColorPickerWithPalette(1, "标点颜色", ref marker.Color); ImGui.SameLine(); ImGui.TextUnformatted("标点颜色");
            ImGui.SameLine();
            changed |= ImguiUtils.ColorPickerWithPalette(2, "标点描边颜色", ref marker.StrokeColor); ImGui.SameLine(); ImGui.TextUnformatted("标点描边颜色");
            changed |= ImGui.Checkbox("显示名字", ref marker.ShowName);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80f);
            changed |= ImGui.InputInt("优先级", ref marker.Priority, 1);
            ImGuiGroup.EndGroupBox();
        }
        return changed;
    }
}
