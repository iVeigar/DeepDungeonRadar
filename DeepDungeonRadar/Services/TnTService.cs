using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Extensions;
using DeepDungeonRadar.UI;
using ImGuiNET;

namespace DeepDungeonRadar.Services;

// TnT = Trap and Treasure
public sealed class TnTService : IDisposable
{
    private readonly Configuration config;

    private readonly HashSet<Vector2> HoardBlackList = new();

    private readonly HashSet<Vector2> TrapBlacklist = new();

    public static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; set; } = null!;

    public TnTService()
    {
        config = PluginService.Config;
        DeepDungeonObjectLocationEqual = new DeepDungeonObjectLocationEqualityComparer();
        PluginService.PluginInterface.UiBuilder.Draw += OnUpdate;
        PluginService.ClientState.TerritoryChanged += TerritoryChanged;
    }

    private void TerritoryChanged(object? sender, ushort e)
    {
        PluginLog.Information($"territory changed to: {e}");
        TrapBlacklist.Clear();
        HoardBlackList.Clear();
    }

    public void Dispose()
    {
        PluginService.ClientState.TerritoryChanged -= TerritoryChanged;
        PluginService.PluginInterface.UiBuilder.Draw -= OnUpdate;
        PluginLog.Information($"TnT Service unloaded");
    }

    public void OnUpdate()
    {
        if (PluginService.ObjectTable == null) return;
        if (!PluginService.Condition[ConditionFlag.InDeepDungeon]) return;
        EnumerateAllObjects();
        if (config.DeepDungeon_EnableTrapView)
        {
            DrawDeepDungeonObjects();
        }
    }

    private void EnumerateAllObjects()
    {
        foreach (var o in PluginService.ObjectTable)
        {
            var location = o.Location2D();
            if (o.IsSilverCoffer())
            {
                TrapBlacklist.Add(location); // TODO 测试银箱子炸弹到底算不算触发了的陷阱（6388）//不管怎样都不计入陷阱地点记录
            }
            else if (o.IsAccursedHoard() && !HoardBlackList.Contains(location))
            {
                DeepDungeonObject deepDungeonObject = new()
                {
                    Type = DeepDungeonObjectType.AccursedHoard,
                    Base = o.DataId,
                    InstanceId = o.ObjectId,
                    Location = o.Position,
                    Territory = PluginService.ClientState.TerritoryType
                };
                if (config.DeepDungeonObjects.Add(deepDungeonObject))
                {
                    HoardBlackList.Add(location);
                    PluginLog.Information($"New AccursedHoard recorded! {deepDungeonObject}");
                }
            }
            else if (o.IsTrap() && !TrapBlacklist.Contains(location))
            {
                DeepDungeonObject deepDungeonObject2 = new()
                {
                    Type = DeepDungeonObjectType.Trap,
                    Base = o.DataId,
                    InstanceId = o.ObjectId,
                    Location = o.Position,
                    Territory = PluginService.ClientState.TerritoryType
                };
                if (config.DeepDungeonObjects.Add(deepDungeonObject2))
                {
                    TrapBlacklist.Add(location);
                    PluginLog.Information($"New Trap recorded! {deepDungeonObject2}");
                }
            }
        }
    }

    private void DrawDeepDungeonObjects()
    {
        var backgroundDrawList = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
        float radius;
        uint color;
        var meWorldPos = PluginService.ClientState.LocalPlayer.Position;
        foreach (var grouping in (from i in config.DeepDungeonObjects
                                  where i.Territory != 0 && i.Bg == Util.TerritoryToBg(PluginService.ClientState.TerritoryType) && i.Location.Distance2D(meWorldPos) < config.DeepDungeon_ObjectShowDistance
                                  select i).GroupBy(i => i, DeepDungeonObjectLocationEqual))
        {
            if (grouping.Key.Type == DeepDungeonObjectType.Trap)
            {
                radius = 1.7f;
                color = Color.Red;
            }
            else// if (grouping.Key.Type == DeepDungeonType.AccursedHoard)
            {
                radius = 2.0f;
                color = Color.Yellow;
            }

            if (config.DeepDungeon_ShowObjectCount)
            {
                backgroundDrawList.DrawRingWorldWithText(grouping.Key.Location, radius, 2f, color, grouping.Count().ToString(), new Vector2(0f, 0f - ImGui.GetTextLineHeight()));
            }
            else
            {
                backgroundDrawList.DrawRingWorld(grouping.Key.Location, radius, 2f, color);
            }
        }
    }
}
