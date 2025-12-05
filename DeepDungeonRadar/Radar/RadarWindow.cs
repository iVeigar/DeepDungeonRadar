using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Data;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace DeepDungeonRadar.Radar;

public sealed class RadarWindow : Window
{
    private readonly Configuration config = Plugin.Config;
    private readonly DeepDungeonService deepDungeonService;
    private readonly MapService mapService;
    private readonly ISharedImmediateTexture Arrow = Svc.Texture.GetFromGameIcon(60541);
    private List<(Vector2 WorldPos, uint Color, uint StrokeColor, string Name, int Priority)> RadarDrawList { get; } = [];
    private List<Vector2> PassageMarkers { get; } = [];
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

    public RadarWindow(DeepDungeonService deepDungeonService, MapService mapService)
        : base("Deep Dungeon Radar Show", ImGuiWindowFlags.None)
    {
        this.deepDungeonService = deepDungeonService;
        this.mapService = mapService;
        Size = new Vector2(360, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
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
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertU32ToFloat4(config.RadarWindowBgColor));
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
        PassageMarkers.Clear();
        RadarDrawList.Clear();
    }

    public unsafe override void Update()
    {
        if (Svc.Objects == null)
            return;

        var passages = deepDungeonService.GetPassageRooms();
        foreach (var o in Svc.Objects.Skip(1))
        {
            Marker markerCfg;
            if (o.IsPlayer())
            {
                markerCfg = config.Markers.Player;
            }
            else if (o.IsMob(out var b))
            {
                if (b.IsHelpfulNpc())
                {
                    markerCfg = config.Markers.Friendly;
                    markerCfg.ShowName = true;
                }
                else if (!b.IsDead && b.IsTargetable)
                {
                    if (b.IsKerrigan())
                    {
                        markerCfg = config.Markers.Friendly;
                        markerCfg.ShowName = true;
                    }
                    else 
                    {
                        markerCfg = config.Markers.Enemy;
                        if (b.IsMimic())
                            markerCfg.ShowName = true;
                    }
                }
                else
                    continue;
            }
            else if (o.IsPassage())
            {
                markerCfg = config.Markers.EventObj;
                // 视野内的传送装置使用精确位置画箭头
                passages.Remove(mapService.PositionToRoomIndex(o.Position2D()));
                PassageMarkers.Add(o.Position2D());

            }
            else if (o.IsReturn())
            {
                if (Svc.Party.Length == 0) continue;
                markerCfg = config.Markers.EventObj;
            }
            else if (o.IsVotive())
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
        foreach (var roomIdx in passages)
        {
            // 视野外的传送装置使用所在房间的大概位置画箭头
            var position = mapService.RoomIndexToPosition(roomIdx);
            if (position != default)
            {
                PassageMarkers.Add(position);
            }
        }
    }

    public unsafe override void Draw()
    {
        var camera = CameraManager.Instance()->CurrentCamera;
        if (camera == null || !Player.Available)
            return;

        var cameraRotation = MathF.Atan2(camera->ViewMatrix.M13, camera->ViewMatrix.M33);
        var playerRotation = Player.Rotation;
        var radarRotation = config.RadarOrientationFixed ? 0 : cameraRotation;
        var radarRotationVec2 = radarRotation.ToDirection();

        var windowSize = ImGui.GetWindowSize();
        var windowLeftTop = ImGui.GetWindowPos();
        var windowCenter = windowLeftTop + windowSize / 2;
        var meWindowPos = windowCenter;
        var meWorldPos = Player.Position.ToVector2();
        var zoom = windowSize.X * UvZoom / 256f;

        var windowDrawList = ImGui.GetWindowDrawList();
        windowDrawList.ChannelsSplit(3);

        // 画地形图
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

        // 画实体标记
        windowDrawList.ChannelsSetCurrent(1);
        foreach (var (worldpos, color, strokeColor, name, _) in RadarDrawList)
        {
            var pos = worldpos.WorldToWindow(meWorldPos, windowCenter, zoom, radarRotationVec2);
            windowDrawList.DrawDotWithText(pos, name, color, strokeColor);
        }

        var playerConeRad = radarRotation - cameraRotation - MathF.PI * 0.5f;
        var playerMarkerCfg = config.Markers.Player;
        windowDrawList.DrawDotWithText(windowCenter, playerMarkerCfg.ShowName ? "我" : null, playerMarkerCfg.Color, playerMarkerCfg.StrokeColor);
        windowDrawList.PathArcTo(windowCenter, zoom * 25f, playerConeRad - MathF.PI * 0.25f, playerConeRad + MathF.PI * 0.25f, 24);
        windowDrawList.PathLineTo(windowCenter);
        windowDrawList.PathStroke(playerMarkerCfg.Color, ImDrawFlags.Closed, 1f);

        // 画辅助圈
        windowDrawList.AddCircle(windowCenter, zoom * 80f, config.RadarSenseCircleOutlineColor, 100);

        // 画传送装置方位箭头
        var arrowTex = Arrow.GetWrapOrDefault();
        if (arrowTex == null)
            goto DrawButton;

        var arrowScale = config.RadarPassageArrowScale;
        var arrowSize = new Vector2(25, 25) * zoom * arrowScale;
        var arrowCenter = meWindowPos + new Vector2(0, -80) * zoom;
        var arrowLT = arrowCenter + arrowSize * new Vector2(-0.5f, -0.5f);
        var arrowRT = arrowCenter + arrowSize * new Vector2(0.5f, -0.5f);
        var arrowRB = arrowCenter + arrowSize * new Vector2(0.5f, 0.5f);
        var arrowLB = arrowCenter + arrowSize * new Vector2(-0.5f, 0.5f);
        foreach (var passagePos in PassageMarkers)
        {
            if (passagePos.Distance(meWorldPos) < 80f)
                continue;
            var rotation = radarRotation - (meWorldPos - passagePos).ToRad();
            windowDrawList.AddImageQuad(arrowTex.Handle,
                arrowLT.RotateAround(meWindowPos, rotation),
                arrowRT.RotateAround(meWindowPos, rotation),
                arrowRB.RotateAround(meWindowPos, rotation),
                arrowLB.RotateAround(meWindowPos, rotation), 
                config.Markers.EventObj.Color);
        }

        DrawButton:
        // 画操作按钮
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
