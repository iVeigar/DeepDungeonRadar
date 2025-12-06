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
    private bool changed = false;
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
        DrawTabMarkerSettings();
    }

    public override void OnClose()
    {
        if (changed)
        {
            config.Save();
            changed = false;
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

        changed |= ImGui.Checkbox("固定指北", ref config.RadarOrientationFixed);
        changed |= ImGui.Checkbox("鼠标穿透", ref config.RadarClickThrough);
        if (!config.RadarClickThrough)
        {
            ImGui.Indent();
            changed |= ImGui.Checkbox("锁定窗口位置和尺寸##RadarLockSizePos", ref config.RadarLockSizePos);
            ImGui.Unindent();
        }
        changed |= ImGui.Checkbox("显示碰撞盒标记点", ref config.ShowColliderBoxDot);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("房间出口地面上的绿/红圆点，表示该处碰撞盒可穿行/不可穿行");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGuiGroup.BeginGroupBox("地图"))
        {
            using var id = ImRaii.PushId("地图");

            ImGui.BeginGroup();
            changed |= ImGuiUtils.ColorPickerWithPalette(1, "地图窗口背景", ref config.RadarWindowBgColor);
            ImGui.SameLine();
            ImGui.Text("地图窗口背景");
            changed |= ImGuiUtils.ColorPickerWithPalette(2, "可达区域边界", ref config.ReachableAreaBorderColor);
            ImGui.SameLine();
            ImGui.Text("可达区域边界");
            changed |= ImGuiUtils.ColorPickerWithPalette(3, "可达区域背景", ref config.ReachableAreaBackgroundColor);
            ImGui.SameLine();
            ImGui.Text("可达区域背景");
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            changed |= ImGui.Checkbox("绘制不可达区域", ref config.RadarDrawUnreachable);
            changed |= ImGuiUtils.ColorPickerWithPalette(4, "不可达区域边界", ref config.UnreachableAreaBorderColor);
            ImGui.SameLine();
            ImGui.Text("不可达区域边界");
            changed |= ImGuiUtils.ColorPickerWithPalette(5, "不可达区域背景", ref config.UnreachableAreaBackgroundColor);
            ImGui.SameLine();
            ImGui.Text("不可达区域背景");
            ImGui.EndGroup();

            ImGuiGroup.EndGroupBox();
        }
    }

    private void DrawTabMarkerSettings()
    {
        using var tabItem = ImRaii.TabItem("记号");
        if (!tabItem) return;
        ImGui.BulletText("优先级更高的标记会绘制在图层更上层\n本地玩家的标记始终置顶");
        ImGui.BulletText("图标标记仅支持宝箱、宝藏、装置和本地玩家\n其他实体仍使用标点+名字");
        ImGui.BulletText("辅助圈半径80y，游戏对象基本上会在进入范围前就显示在雷达上\n可以用这个特性快速找到宝藏房、烛台房");
        ImGui.Separator();
        if (ImGuiGroup.BeginGroupBox("小地图标记"))
        {
            ImGui.Text("地图标记类型：");
            ImGui.SameLine();
            ImGui.RadioButton("游戏图标", ref config.RadarObjectUseIcons, true);
            ImGui.SameLine();
            ImGui.RadioButton("标点+名字", ref config.RadarObjectUseIcons, false);

            ImGui.Separator();

            changed |= ImGui.SliderFloat("实体标记点大小", ref config.MarkerDotSize, 2f, 15f);
            ImGui.Spacing();
            changed |= ImGui.Checkbox("移除实体名称前缀", ref config.RemoveNamePrefix);
            ImGui.Text("名称前缀");
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("每行一个前缀");
            var height = ImGui.CalcTextSize("字").Y;
            changed |= ImGui.InputTextMultiline(string.Empty, ref config.NamePrefixes, 512, new(0, height * 4 + 2));

            ImGui.Separator();

            ImGui.Text("图标大小比例");
            changed |= ImGui.SliderFloat("本地玩家", ref config.IconScales.LocalPlayer, 0.1f, 3f);
            changed |= ImGui.SliderFloat("宝箱", ref config.IconScales.Chest, 0.1f, 3f);
            changed |= ImGui.SliderFloat("埋藏的宝藏", ref config.IconScales.AccursedHoard, 0.1f, 3f);
            changed |= ImGui.SliderFloat("装置(传送、再生、烛台)", ref config.IconScales.EventObj, 0.1f, 3f);
            changed |= ImGui.SliderFloat("传送房方向指示器", ref config.IconScales.PassageArrow, 0.1f, 3f);

            ImGuiGroup.EndGroupBox();
        }

        
        ImGui.Separator();



        ImGui.Spacing();
        if (ImGuiGroup.BeginGroupBox("标记颜色和优先级"))
        {
            changed |= ImGuiUtils.ColorPickerWithPalette(0, "辅助圈", ref config.RadarSenseCircleOutlineColor); ImGui.SameLine(); ImGui.TextUnformatted("辅助圈颜色");
            changed |= DrawMarkerConfig("玩家", ref config.Markers.Player);
            changed |= DrawMarkerConfig("敌对", ref config.Markers.Enemy);
            changed |= DrawMarkerConfig("友好", ref config.Markers.Friendly);
            changed |= DrawMarkerConfig("装置", ref config.Markers.EventObj, true);
            changed |= DrawMarkerConfig("金箱", ref config.Markers.GoldChest, true);
            changed |= DrawMarkerConfig("银箱", ref config.Markers.SilverChest, true);
            changed |= DrawMarkerConfig("铜箱", ref config.Markers.BronzeChest, true);
            ImGuiGroup.EndGroupBox();
        }
        ImGui.Spacing();


    }

    private static bool DrawMarkerConfig(string id, ref Marker marker, bool supportIcon = false)
    {
        var changed = false;
        using var a = ImRaii.PushId(id);
        changed |= ImGuiUtils.ColorPickerWithPalette(1, "标点颜色", ref marker.Color); ImGui.SameLine(); ImGui.TextUnformatted(id);
        ImGui.SameLine();            
        ImGui.SetNextItemWidth(80f);
        changed |= ImGui.InputInt(string.Empty, ref marker.Priority, 1);
        return changed;
    }
}
