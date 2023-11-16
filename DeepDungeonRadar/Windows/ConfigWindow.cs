using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.UI;
using ImGuiNET;

namespace DeepDungeonRadar.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private HashSet<DeepDungeonObject> deepDungeonObjectsImportCache;
    private bool importingError = false;
    private string errorMessage = string.Empty;
    private int treeLevel;

    public ConfigWindow() : base("Deep Dungeon Radar Config", ImGuiWindowFlags.None)
    {
        Size = new(480f, 640f);
        SizeCondition = ImGuiCond.FirstUseEver;
        config = Service.Config;
        deepDungeonObjectsImportCache = new();
    }

    public void Dispose()
    {

    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("tabbar", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("雷达"))
            {
                DrawRadarTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("深层迷宫记录"))
            {
                DrawDeepDungeonRecordTab();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawRadarTab()
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

        var radarLockSizePos = config.RadarLockSizePos;
        if (ImGui.Checkbox("锁定窗口位置和尺寸##RadarLockSizePos", ref radarLockSizePos))
        {
            config.RadarLockSizePos = radarLockSizePos;
            config.Save();
        }

        var radarClickThrough = config.RadarClickThrough;
        if (ImGui.Checkbox("鼠标穿透##RadarClickThrough", ref radarClickThrough))
        {
            config.RadarClickThrough = radarClickThrough;
            config.Save();
        }

        var radarShowInfo = config.RadarShowInfo;
        if (ImGui.Checkbox("显示缩放比例和坐标信息##RadarShowInfo", ref radarShowInfo))
        {
            config.RadarShowInfo = radarShowInfo;
            config.Save();
        }

        var radarMapColor = ImGui.ColorConvertU32ToFloat4(config.RadarMapColor);
        ImGui.TextUnformatted("地图颜色");
        ImGui.SameLine();
        ImguiUtil.ColorPickerWithPalette(1, string.Empty, ref radarMapColor, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
        uint color = ImGui.ColorConvertFloat4ToU32(radarMapColor);
        if (color != config.RadarMapColor)
        {
            config.RadarMapColor = color;
            config.Save();
        }

        ImGui.Separator();
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

    private void DrawDeepDungeonRecordTab()
    {
        ImGui.TextWrapped("记录并显示本机深层迷宫攻略过程中出现过的陷阱与宝藏位置。\n你也可以导出自己的记录并与他人共享情报。");

        var deepDungeon_EnableTrapView = config.DeepDungeon_EnableTrapView;
        if (ImGui.Checkbox("深层迷宫实体显示", ref deepDungeon_EnableTrapView))
        {
            config.DeepDungeon_EnableTrapView = deepDungeon_EnableTrapView;
            config.Save();
        }

        var deepDungeon_ShowObjectCount = config.DeepDungeon_ShowObjectCount;
        if (ImGui.Checkbox("显示计数", ref deepDungeon_ShowObjectCount))
        {
            config.DeepDungeon_ShowObjectCount = deepDungeon_ShowObjectCount;
            config.Save();
        }

        var deepDungeon_ObjectShowDistance = config.DeepDungeon_ObjectShowDistance;
        if (ImGui.SliderFloat("最远显示距离", ref deepDungeon_ObjectShowDistance, 15f, 500f, deepDungeon_ObjectShowDistance.ToString("##.0m"), ImGuiSliderFlags.Logarithmic))
        {
            config.DeepDungeon_ObjectShowDistance = deepDungeon_ObjectShowDistance;
            config.Save();
        }
        ImGui.Separator();
        if (ImGui.Button("导出当前记录点到剪贴板"))
        {
            PluginLog.Information("exporting...");
            PluginLog.Information($"exported {(from i in Service.Config.DeepDungeonObjects group i by i.Territory).Count()} territories, {config.DeepDungeonObjects.Count(i => i.Type == DeepDungeonObjectType.Trap)} traps, {config.DeepDungeonObjects.Count(i => i.Type == DeepDungeonObjectType.AccursedHoard)} hoards.");
            ImGui.SetClipboardText(Service.Config.DeepDungeonObjects.ToCompressedString());
        }
        if (!deepDungeonObjectsImportCache.Any())
        {
            ImGui.SameLine();
            if (ImGui.Button("从剪贴板导入已有的记录点"))
            {
                importingError = false;
                try
                {
                    HashSet<DeepDungeonObject>? source = ImGui.GetClipboardText().DecompressStringToObject<HashSet<DeepDungeonObject>>();
                    if (source != null && source.Any())
                    {
                        deepDungeonObjectsImportCache = source;
                    }
                }
                catch (Exception ex)
                {
                    importingError = true;
                    PluginLog.Warning(ex, "error when importing deep dungeon object list.");
                    errorMessage = ex.Message;
                }
            }
            if (importingError)
            {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "导入发生错误，请检查导入的字符串和日志。");
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), errorMessage);
            }
            return;
        }
        ImGui.SameLine();
        if (ImGui.Button("正在准备导入..."))
        {
            deepDungeonObjectsImportCache.Clear();
            PluginLog.Debug("user canceled importing task.", Array.Empty<object>());
            return;
        }
        bool treeLevelChanged = ImGui.SliderInt("树视图展开级别", ref treeLevel, 1, 4, getformat(treeLevel));
        var bgGroups = from i in deepDungeonObjectsImportCache
                       group i by Util.TerritoryToBg(i.Territory) into i
                       orderby i.Key
                       select i;
        ImGui.TextWrapped($"将要导入 {bgGroups.Count()} 个区域的 {deepDungeonObjectsImportCache.Count} 条记录。\n包含 " +
            $"{bgGroups.Select(i => (from j in i
                                     where j.Type == DeepDungeonObjectType.Trap
                                     group j by j.Location2D).Count()).Sum()} 个陷阱位置，" +
            $"{bgGroups.Select(i => (from j in i
                                     where j.Type == DeepDungeonObjectType.AccursedHoard
                                     group j by j.Location2D).Count()).Sum()} 个宝藏位置。");
        if (ImGui.BeginChild("deepdungeonobjecttreeview##Radar", new Vector2(-1f, (0f - ImGui.GetFrameHeightWithSpacing()) * 2f), true))
        {
            foreach (var bgGroup in bgGroups)
            {
                if (treeLevelChanged)
                {
                    ImGui.SetNextItemOpen(treeLevel > 1);
                }
                if (ImGui.TreeNodeEx($"{bgGroup.Key.GetName()}##DDTerritoryKey", ImGuiTreeNodeFlags.Framed))
                {
                    foreach (var typeGroup in from i in bgGroup
                                              group i by i.Type into i
                                              orderby i.Key
                                              select i)
                    {
                        if (treeLevelChanged)
                        {
                            ImGui.SetNextItemOpen(treeLevel > 2);
                        }
                        var locationGroups = typeGroup.GroupBy(i => i.Location2D);
                        if (ImGui.TreeNodeEx($"{typeGroup.Key} ({locationGroups.Count()})##{bgGroup.Key}", ImGuiTreeNodeFlags.SpanAvailWidth))
                        {
                            foreach (var locationGroup in locationGroups.OrderByDescending(i => i.Count()))
                            {
                                if (treeLevelChanged)
                                {
                                    ImGui.SetNextItemOpen(treeLevel > 3);
                                }
                                if (ImGui.TreeNodeEx($"{locationGroup.Key} ({locationGroup.Count()})##{typeGroup.Key}{bgGroup.Key}", ImGuiTreeNodeFlags.SpanAvailWidth))
                                {
                                    foreach (var ddobj in locationGroup.OrderBy(i => i.InstanceId))
                                    {
                                        ImGui.TextUnformatted($"{ddobj.Territory} : {ddobj.Base} : {ddobj.InstanceId:X}");
                                    }
                                    ImGui.TreePop();
                                }
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.EndChild();
        }
        ImGui.TextColored(new Vector4(1f, 0.8f, 0f, 1f), "确认后数据将合并到本机记录且不可撤销，请确认数据来源可靠。要继续吗？");
        ImGui.Spacing();
        if (ImGui.Button("取消导入##importDecline"))
        {
            deepDungeonObjectsImportCache.Clear();
            PluginLog.Debug("user canceled importing task.");
            return;
        }
        ImGui.SameLine();
        if (ImGui.Button("确认导入##importAccept"))
        {
            int count = config.DeepDungeonObjects.Count;
            config.DeepDungeonObjects.UnionWith(deepDungeonObjectsImportCache);
            config.Save();
            deepDungeonObjectsImportCache.Clear();
            int num = config.DeepDungeonObjects.Count - count;
            PluginLog.Information($"imported {num} deep dungeon object records.");
        }
        static string getformat(int input)
        {
            return input switch
            {
                0 => "默认",
                1 => "全部折叠",
                2 => "展开到物体类型",
                3 => "展开到物体位置",
                4 => "全部展开",
                _ => "invalid",
            };
        }
    }
}
