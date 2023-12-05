using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Extensions;
using DeepDungeonRadar.Maps;
using DeepDungeonRadar.Services;
using DeepDungeonRadar.UI;
using ImGuiNET;
using ImGuiScene;
namespace DeepDungeonRadar.Windows;

public sealed class RadarWindow : Window, IDisposable
{
    private readonly Configuration config;

    private List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name)> RadarDrawList { get; } = new();

    private static bool CanUseLargeFont => ImGui.GetIO().Fonts.Fonts.Size > 2;
    private bool usingLargeFont = false;

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
    
    public RadarWindow() : base("Deep Dungeon Radar Show", ImGuiWindowFlags.None)
    {
        config = PluginService.Config;
    }

    public void Dispose()
    {
    }

    public override void PreOpenCheck()
    {
        IsOpen = TnTService.InDeepDungeon() && config.RadarEnabled;
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoBackground;
        if (config.RadarClickThrough)
        {
            Flags |= ImGuiWindowFlags.NoMouseInputs;
        }
        if (config.RadarLockSizePos)
        {
            Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        }
    }

    public override unsafe void PreDraw()
    {
        usingLargeFont = CanUseLargeFont && config.RadarUseLargeFont;
        if (usingLargeFont)
        {
            ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowSizeConstraints(new Vector2(150f, 150f), new Vector2(float.MaxValue, float.MaxValue), delegate (ImGuiSizeCallbackData* data)
        {
            var width = Math.Max(data->DesiredSize.X, data->DesiredSize.Y);
            data->DesiredSize = new Vector2(width, width);
        });
    }

    public override void Draw()
    {
        if (PluginService.ObjectTable == null) return;
        EnumerateAllObjects();
        DrawRadar();
    }

    public override void PostDraw()
    {
        if (usingLargeFont)
        {
            ImGui.PopFont();
        }
        ImGui.PopStyleVar();
        RadarDrawList.Clear();
    }

    private void EnumerateAllObjects()
    {
        if (!TnTService.InDeepDungeon())
            return;

        var first = true;
        foreach (var o in PluginService.ObjectTable)
        {
            if (first)
            {
                first = false;
                continue;
            }
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
                item = string.IsNullOrEmpty(o.GetDictionaryName()) ? $"{o.ObjectKind} {o.DataId}" : $"{o.GetDictionaryName()} {o.Position.Distance2D(TnTService.MeWorldPos):F2}m";
                break;
        }
        RadarDrawList.Add((o.Position, fgcolor, bgcolor, item));
    }

    internal unsafe void DrawRadar()
    {
        var windowSize = ImGui.GetWindowSize();
        var windowLeftTop = ImGui.GetWindowPos();
        var windowCenter = windowLeftTop + windowSize / 2;
        var zoom = windowSize.X * UvZoom / 2048f;
        var rotation = config.RadarOrientationFixed ? 0 : -PluginService.Address.HRotation;
        var windowDrawList = ImGui.GetWindowDrawList();
        windowDrawList.ChannelsSplit(3);

        // Draw map
        windowDrawList.ChannelsSetCurrent(0);
        windowDrawList.DrawCurrentTerrytoryMap(TnTService.MeWorldPos, windowCenter, zoom, rotation, config.RadarMapColor);

        // Draw dots of objects 
        windowDrawList.ChannelsSetCurrent(1);
        foreach (var (worldpos, fgcolor, bgcolor, name) in RadarDrawList)
        {
            Vector2 pos = worldpos.ToRadarWindowPos(TnTService.MeWorldPos, windowCenter, zoom, rotation);
            windowDrawList.DrawMapTextDot(pos, name, fgcolor, bgcolor);
        }
        if (config.RadarShowCenter)
        {
            windowDrawList.DrawMapTextDot(windowCenter, config.RadarDetailLevel > 0 ? "我" : null, Color.Cyan, Color.Black);
            if (config.RadarShowAssistCircle)
            {
                var meRotation = config.RadarOrientationFixed ? -PluginService.Address.HRotation : 0f;
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
            var text = $"{windowSize.X / 2f / zoom:F2}m X: {TnTService.MeWorldPos.X:N3} Y: {TnTService.MeWorldPos.Y:N3} Z: {TnTService.MeWorldPos.Z:N3}";
            windowDrawList.DrawTextWithBg(windowLeftTop + ImGui.GetWindowSize() - ImGui.CalcTextSize(text), text, Color.White, Color.TransBlack, false);
        }
        if (!config.RadarClickThrough)
        {
            ImGui.SetCursorPos(new Vector2(5f, 5f));
            var icon = config.RadarOrientationFixed ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow;
            if (ImguiUtil.IconButton(icon, "ToggleSnap", new Vector2(25f, 25f)))
            {
                config.RadarOrientationFixed ^= true;
                config.Save();
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
    }
}
