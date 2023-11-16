using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Logging;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Extensions;
using DeepDungeonRadar.Maps;
using DeepDungeonRadar.UI;
using ImGuiNET;
using ImGuiScene;
namespace DeepDungeonRadar.Windows;

public sealed class RadarUI : IDisposable
{
    private readonly Configuration config;

    private static Vector3 _meWorldPos = Vector3.Zero;



    public const ImGuiTableFlags TableFlags = ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY;

    private readonly HashSet<Vector2> HoardBlackList = new();

    private readonly HashSet<Vector2> TrapBlacklist = new();

    private List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name)> RadarDrawList { get; } = new();

    private static int FontsSize => ImGui.GetIO().Fonts.Fonts.Size;

    public static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; set; } = null!;

    private float uvZoom1 = 8f;

    private ref float UvZoom
    {
        get
        {
            if (uvZoom1 < 1f)
            {
                uvZoom1 = 1f;
            }
            return ref uvZoom1;
        }
    }
    internal readonly string[] specialObjectNames = { "传送石冢", "再生石冢", "传送灯笼", "再生灯笼", "传送装置", "再生灯笼" };
    public RadarUI()
    {
        config = Service.Config;
        DeepDungeonObjectLocationEqual = new DeepDungeonObjectLocationEqualityComparer();
        Service.ClientState.TerritoryChanged += TerritoryChanged;
    }

    private void TerritoryChanged(object? sender, ushort e)
    {
        PluginLog.Information($"territory changed to: {e}");
        TrapBlacklist.Clear();
        HoardBlackList.Clear();
    }

    public void Dispose()
    {
        Service.ClientState.TerritoryChanged -= TerritoryChanged;
    }

    public unsafe void Draw()
    {
        if (Service.ObjectTable == null) return;

        EnumerateAllObjects();
        if (config.DeepDungeon_EnableTrapView && Service.Condition[ConditionFlag.InDeepDungeon])
        {
            DrawDeepDungeonObjects();
        }
        bool canUseLargeFont = FontsSize > 2;
        if (canUseLargeFont && config.RadarUseLargeFont)
        {
            ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
        }
        if (config.RadarEnabled && Service.Condition[ConditionFlag.InDeepDungeon])
        {
            DrawRadar();
        }
        if (canUseLargeFont && config.RadarUseLargeFont)
        {
            ImGui.PopFont();
        }
        RadarDrawList.Clear();
    }

    private void EnumerateAllObjects()
    {
        if (!Service.Condition[ConditionFlag.InDeepDungeon])
            return;
        var first = true;

        foreach (var o in Service.ObjectTable)
        {
            if (first)
            {
                first = false;
                _meWorldPos = o.Position;
                continue;
            }
            AddDeepDungeonObjectRecord(o);
            if (config.RadarEnabled)
            {
                uint fgColor;
                uint bgColor = Color.Black;
                if (o.ObjectKind == ObjectKind.BattleNpc && (BattleNpcSubKind)o.SubKind == BattleNpcSubKind.Enemy && !o.IsDead && o.IsTargetable)
                {
                    fgColor = Color.White;
                }
                else if (o.ObjectKind == ObjectKind.EventObj || o.ObjectKind == ObjectKind.Treasure)
                {
                    if (specialObjectNames.Contains(o.Name.ToString()))
                    {
                        fgColor = Color.Cyan;
                    }
                    else if (o.IsTargetable || o.DataId == 2007542U) //埋藏的宝藏
                    {
                        fgColor = 0xFFADDEFF; // TODO hardcode
                    }
                    else
                    {
                        continue;
                    }
                }
                else // TODO 显示队友
                {
                    continue;
                }
                AddObjectToRadarDrawList(o, fgColor, bgColor);
            }
        }
    }

    private void AddDeepDungeonObjectRecord(GameObject o)
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
                Territory = Service.ClientState.TerritoryType
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
                Territory = Service.ClientState.TerritoryType
            };
            if (config.DeepDungeonObjects.Add(deepDungeonObject2))
            {
                TrapBlacklist.Add(location);
                PluginLog.Information($"New Trap recorded! {deepDungeonObject2}");
            }
        }
    }

    private void AddObjectToRadarDrawList(GameObject o, uint fgcolor, uint bgcolor, TextureWrap? icon = null)
    {
        string item = string.Empty;
        switch (config.RadarDetailLevel)
        {
            case DetailLevel.仅图标:
                break;
            case DetailLevel.仅物体名:
                item = string.IsNullOrEmpty(o.GetDictionaryName()) ? $"{o.ObjectKind} {o.DataId}" : o.GetDictionaryName();
                break;
            case DetailLevel.物体名距离:
                item = string.IsNullOrEmpty(o.GetDictionaryName()) ? $"{o.ObjectKind} {o.DataId}" : $"{o.GetDictionaryName()} {o.Position.Distance2D(_meWorldPos):F2}m";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        RadarDrawList.Add((o.Position, fgcolor, bgcolor, item));
    }

    private void DrawDeepDungeonObjects()
    {
        var backgroundDrawList = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
        float radius;
        uint color;
        foreach (var grouping in (from i in config.DeepDungeonObjects
                                  where i.Territory != 0 && i.Bg == Util.TerritoryToBg(Service.ClientState.TerritoryType) && i.Location.Distance2D(_meWorldPos) < config.DeepDungeon_ObjectShowDistance
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

    internal unsafe void DrawRadar()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowSizeConstraints(new Vector2(150f, 150f), new Vector2(float.MaxValue, float.MaxValue), delegate (ImGuiSizeCallbackData* data)
        {
            var width = Math.Max(data->DesiredSize.X, data->DesiredSize.Y);
            data->DesiredSize = new Vector2(width, width);
        });
        var imGuiWindowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoBackground;
        if (config.RadarClickThrough)
        {
            imGuiWindowFlags |= ImGuiWindowFlags.NoMouseInputs;
        }
        if (config.RadarLockSizePos)
        {
            imGuiWindowFlags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        }
        if (ImGui.Begin("Radar#maptex###123", imGuiWindowFlags))
        {
            var windowSize = ImGui.GetWindowSize();
            var windowLeftTop = ImGui.GetWindowPos();
            var windowCenter = windowLeftTop + windowSize / 2;
            var zoom = windowSize.X * UvZoom / 2048f;
            var rotation = config.RadarOrientationFixed ? 0 : -Service.Address.HRotation;
            var windowDrawList = ImGui.GetWindowDrawList();
            windowDrawList.ChannelsSplit(3);

            // Draw map
            windowDrawList.ChannelsSetCurrent(0);
            windowDrawList.DrawCurrentTerrytoryMap(_meWorldPos, windowCenter, zoom, rotation, config.RadarMapColor);

            // Draw dots of objects 
            windowDrawList.ChannelsSetCurrent(1);
            foreach (var (worldpos, fgcolor, bgcolor, name) in RadarDrawList)
            {
                Vector2 pos = worldpos.ToRadarWindowPos(_meWorldPos, windowCenter, zoom, rotation);
                windowDrawList.DrawMapTextDot(pos, name, fgcolor, bgcolor);
            }
            if (config.RadarShowCenter)
            {
                windowDrawList.DrawMapTextDot(windowCenter, config.RadarDetailLevel > 0 ? "我" : null, Color.Cyan, Color.Black);
                if (config.RadarShowAssistCircle)
                {
                    var meRotation = config.RadarOrientationFixed ? -Service.Address.HRotation : 0f;
                    windowDrawList.PathArcTo(windowCenter, zoom * 25f, meRotation - 1.5707964f - 0.7853982f, meRotation - 0.7853982f, 24);
                    windowDrawList.PathLineTo(windowCenter);
                    windowDrawList.PathStroke(Color.Cyan, ImDrawFlags.Closed, 1.5f);
                    windowDrawList.AddCircle(windowCenter, zoom * 125f, Color.Grey, 100);
                }
            }

            // Draw info
            windowDrawList.ChannelsSetCurrent(2);
            if (config.RadarShowInfo)
            {
                var text = $" {windowSize.X / 2f / zoom:F2}m X: {_meWorldPos.X:N3} Y: {_meWorldPos.Y:N3} Z: {_meWorldPos.Z:N3} ";
                windowDrawList.DrawTextWithBg(windowLeftTop + ImGui.GetWindowSize() - ImGui.CalcTextSize(text), text, Color.White, Color.TransBlack, false);
            }
            if (!config.RadarClickThrough)
            {
                ImGui.SetCursorPos(new Vector2(5f, 5f));
                var icon = config.RadarOrientationFixed ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow;
                if (ImguiUtil.IconButton(icon, "ToggleSnap", new Vector2(25f, 25f)))
                {
                    config.RadarOrientationFixed ^= true;
                }
                ImGui.SetCursorPosX(5f);
                if (ImguiUtil.IconButton(FontAwesomeIcon.PlusCircle, "zoom++", new Vector2(25f, 25f)))
                {
                    UvZoom *= 1.1f;
                }
                ImGui.SetCursorPosX(5f);
                if (ImguiUtil.IconButton(FontAwesomeIcon.MinusCircle, "zoom--", new Vector2(25f, 25f)))
                {
                    UvZoom *= 0.9f;
                }
            }
            if (ImGui.IsWindowHovered())
            {
                UvZoom += UvZoom * ImGui.GetIO().MouseWheel * 0.1f;
            }
            windowDrawList.ChannelsMerge();
            ImGui.End();
        }
        ImGui.PopStyleVar();
    }
}
