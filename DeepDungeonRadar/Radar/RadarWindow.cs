using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Data;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace DeepDungeonRadar.Radar;

public sealed class RadarWindow(DeepDungeonService deepDungeonService, MapService mapService) : Window("Deep Dungeon Radar Show", ImGuiWindowFlags.None)
{
    private readonly Configuration config = Plugin.Config;
    private readonly DeepDungeonService deepDungeonService = deepDungeonService;
    private readonly MapService mapService = mapService;

    private List<(Vector2 WorldPos, uint Color, uint StrokeColor, string Name, int Priority)> RadarDrawList { get; } = [];

    private float UvZoom
    {
        get => config.RadarZoom;
        set
        {
            if (value >= 0.1f && value < 5f && config.RadarZoom != value)
            {
                config.RadarZoom = value;
            }
        }
    }

    public override void PreOpenCheck()
    {
        IsOpen = config.RadarEnabled && deepDungeonService.IsRadarReady;
    }

    public override unsafe void PreDraw()
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking;
        if (config.RadarClickThrough)
        {
            Flags |= ImGuiWindowFlags.NoMouseInputs;
        }
        if (config.RadarLockSizePos)
        {
            Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.2f);
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 1f));
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.SetNextWindowSizeConstraints(new Vector2(150f, 150f), new Vector2(float.MaxValue, float.MaxValue), delegate (ImGuiSizeCallbackData* data)
        {
            var width = Math.Max(data->DesiredSize.X, data->DesiredSize.Y);
            data->DesiredSize = new Vector2(width, width);
        });
    }
    
    public override void PostDraw()
    {
        if (!config.RadarLockSizePos)
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        RadarDrawList.Clear();
    }

    public unsafe override void Update()
    {
        if (Svc.Objects == null)
            return;

        foreach (var o in Svc.Objects.Skip(1))
        {
            Marker markerCfg;
            if (o.IsPlayer())
            {
                markerCfg = config.Markers.Player;
            }
            else if (o.IsMob(out var b))
            {
                if (b.IsFriendly())
                {
                    markerCfg = config.Markers.Friendly;
                    markerCfg.ShowName = true;
                }
                else if (!b.IsDead && b.IsTargetable)
                {
                    markerCfg = config.Markers.Enemy;
                    if (b.IsMimic() || b.IsKerrigan())
                        markerCfg.ShowName = true;
                }
                else
                    continue;
            }
            // todo 设置单刷时不显示再生装置
            else if (o.IsPassage() || o.IsReturn())
            {
                markerCfg = config.Markers.EventObj;
            }
            else if (o.IsCandelabra())
            {
                if (!o.IsTargetable) continue;
                markerCfg = config.Markers.EventObj;
            }
            else if (o.IsGoldChest())
            {
                if (!o.IsTargetable) continue;
                markerCfg = config.Markers.GoldChest;
            }
            else if (o.IsSilverChest())
            {
                if (!o.IsTargetable) continue;
                markerCfg = config.Markers.SilverChest;
            }
            else if (o.IsBronzeChest())
            {
                if (o.IsChestOpenedOrFaded()) continue;
                markerCfg = config.Markers.BronzeChest;
            }
            else if (o.IsMimicChest())
            {
                markerCfg = config.Markers.BronzeChest with { ShowName = true };
            }
            else if (o.IsAccursedHoard())
            {
                if (deepDungeonService.AccursedHoardOpened) continue;
                markerCfg = config.Markers.AccursedHoard;
            }
            else
            {
                continue;
            }

            var name = markerCfg.ShowName ? o.GetDisplayName() : string.Empty;
            RadarDrawList.Add((o.Position2D(), markerCfg.Color, markerCfg.StrokeColor, name, markerCfg.Priority));
        }
        RadarDrawList.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public unsafe override void Draw()
    {
        var camera = CameraManager.Instance()->CurrentCamera;
        if (camera == null)
            return;
        if (Svc.ClientState.LocalPlayer == null)
            return;
        var cameraRotation = MathF.Atan2(camera->ViewMatrix.M13, camera->ViewMatrix.M33);
        var playerRotation = Svc.ClientState.LocalPlayer!.Rotation;
        var radarRotation = config.RadarOrientationFixed ? 0 : cameraRotation;
        var radarRotationVec2 = radarRotation.ToNormalizedVector2();

        var windowSize = ImGui.GetWindowSize();
        var windowLeftTop = ImGui.GetWindowPos();
        var windowCenter = windowLeftTop + windowSize / 2;
        var meWindowPos = windowCenter;
        var meWorldPos = Player.Position.ToVector2();
        var zoom = windowSize.X * UvZoom / 256f;

        var windowDrawList = ImGui.GetWindowDrawList();
        windowDrawList.ChannelsSplit(3);

        // Draw map
        windowDrawList.ChannelsSetCurrent(0);
        if (mapService.IsMapReadyToDraw())
        {
            var info = mapService.CurrentMap.Info;
            windowDrawList.AddImageQuad(mapService.ColoredMapTexture.Handle,
                info.TopLeft.WorldToWindow(meWorldPos, meWindowPos, zoom, radarRotationVec2),
                info.TopRight.WorldToWindow(meWorldPos, meWindowPos, zoom, radarRotationVec2),
                info.BottomRight.WorldToWindow(meWorldPos, meWindowPos, zoom, radarRotationVec2),
                info.BottomLeft.WorldToWindow(meWorldPos, meWindowPos, zoom, radarRotationVec2)
            );
        }

        // Draw markers
        windowDrawList.ChannelsSetCurrent(1);
        foreach (var (worldpos, color, strokeColor, name, _) in RadarDrawList)
        {
            var pos = worldpos.WorldToWindow(Player.Position.ToVector2(), windowCenter, zoom, radarRotationVec2);
            windowDrawList.DrawDotWithText(pos, name, color, strokeColor);
        }

        var playerConeRad = radarRotation - cameraRotation - MathF.PI * 0.5f;
        var playerMarkerCfg = config.Markers.Player;
        windowDrawList.DrawDotWithText(windowCenter, playerMarkerCfg.ShowName ? "我" : null, playerMarkerCfg.Color, playerMarkerCfg.StrokeColor);
        windowDrawList.PathArcTo(windowCenter, zoom * 25f, playerConeRad - MathF.PI * 0.25f, playerConeRad + MathF.PI * 0.25f, 24);
        windowDrawList.PathLineTo(windowCenter);
        windowDrawList.PathStroke(playerMarkerCfg.Color, ImDrawFlags.Closed, 1f);

        windowDrawList.AddCircle(windowCenter, zoom * 80f, config.RadarSenseCircleOutlineColor, 100);

        // Draw buttons
        windowDrawList.ChannelsSetCurrent(2);
        if (!config.RadarClickThrough)
        {
            ImGui.SetCursorPos(new Vector2(5f, 5f));
            var icon = config.RadarOrientationFixed ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow;
            if (ImguiUtils.IconButton(icon, "ToggleSnap", new Vector2(25f, 25f)))
            {
                config.RadarOrientationFixed ^= true;
            }
            ImGui.SetCursorPosX(5f);
            if (ImguiUtils.IconButton(FontAwesomeIcon.PlusCircle, "zoom++", new Vector2(25f, 25f)))
            {
                UvZoom += 0.1f;
            }
            ImGui.SetCursorPosX(5f);
            if (ImguiUtils.IconButton(FontAwesomeIcon.MinusCircle, "zoom--", new Vector2(25f, 25f)))
            {
                UvZoom -= 0.1f;
            }
        }
            if (ImGui.IsWindowHovered())
            {
                UvZoom += ImGui.GetIO().MouseWheel * 0.1f;
            }
        windowDrawList.ChannelsMerge();
    }
}
