using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace DeepDungeonRadar.Radar;

public sealed class RadarWindow : Window
{
    private readonly Configuration config = Plugin.Config;
    private readonly DeepDungeonService deepDungeonService;
    private readonly MapService mapService;
    private readonly TexturePart ArrowIcon = new(60545);
    private readonly TexturePart Return0MiniIcon = new(60905);
    private readonly TexturePart Return10MiniIcon = new(60906);
    private readonly TexturePart Passage0MiniIcon = new(60907);
    private readonly TexturePart Passage10MiniIcon = new(60908);
    private readonly TexturePart BronzeChestIcon = new(60911);
    private readonly TexturePart SilverChestIcon = new(60912);
    private readonly TexturePart GoldChestIcon = new(60913);
    private readonly TexturePart VotiveIcon = new(63988);
    private readonly TexturePart HomeFlagTex = new("ui/uld/DeepDungeonNaviMap.tex", new(64, 64), new(32, 32), new(16, 16));
    private readonly TexturePart FovConeTex = new("ui/uld/navimap.tex", new(352, 0), new(96, 96), new(18, 78));
    private readonly TexturePart PlayerArrowTex = new("ui/uld/DeepDungeonNaviMap.tex", new(0, 64), new(32, 32), new(16, 19));
    private readonly TexturePart TreasureGlowTex = new("ui/uld/navimap.tex", new(384, 120), new(40, 40), new(20, 20));
    private readonly TexturePart AccursedHoardTex = new("ui/uld/DeepDungeonNaviMap.tex", new(108, 112), new(24, 28), new(11, 18));
    private List<RadarObject> RadarObjList { get; } = [];
    private List<Vector2> PassageMarkers { get; } = [];
    private float Zoom
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

    // drawing
    private float cameraRotation;
    private float playerRotation;
    private float radarRotation;
    private float radarScale;
    private Vector2 meWorldPos;
    private Vector2 meRadarPos;
    private Matrix3x2 worldToRadarMatrix;
    private ImDrawListPtr radarWindow;

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
        RadarObjList.Clear();
    }

    public unsafe override void Update()
    {
        if (Svc.Objects == null)
            return;

        var passages = deepDungeonService.GetPassageRooms();
        foreach (var o in Svc.Objects.Skip(1))
        {
            var radarObj = new RadarObject(o, deepDungeonService);
            if (radarObj.Kind == RadarObject.RadarObjectKind.Passage)
            {
                // 视野内的传送装置使用精确位置画箭头
                if (passages.Count == 1)
                    passages.Clear();
                else
                    passages.Remove(mapService.PositionToRoomIndex(radarObj.Position));
                PassageMarkers.Add(radarObj.Position);

            }
            if (radarObj.ShouldDraw())
                RadarObjList.Add(radarObj);
        }
        RadarObjList.Sort((a, b) => a.GetMarkerConfig().Priority.CompareTo(b.GetMarkerConfig().Priority));
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

    private unsafe bool UpdateRadarVars()
    {
        var camera = CameraManager.Instance()->CurrentCamera;
        if (camera == null || !Player.Available)
            return false;

        cameraRotation = MathF.Atan2(camera->ViewMatrix.M13, camera->ViewMatrix.M33);
        playerRotation = Player.Rotation;
        radarRotation = config.RadarOrientationFixed ? 0 : -cameraRotation;

        var windowSize = ImGui.GetWindowSize();
        var windowLeftTop = ImGui.GetWindowPos();
        var windowCenter = windowLeftTop + windowSize / 2;
        meRadarPos = windowCenter;
        meWorldPos = Player.Position.ToVector2();

        radarScale = windowSize.X * Zoom / 256f;

        worldToRadarMatrix = ImGuiUtils.BuildTransformMatrix(meWorldPos, meRadarPos, radarScale, radarRotation);
        radarWindow = ImGui.GetWindowDrawList();
        return true;
    }

    public override void Draw()
    {
        if (!UpdateRadarVars()) 
            return;
        // 画地形图
        DrawFloorMap();

        // 画实体标记
        DrawRadarObjects();

        // 画本地玩家
        DrawLocalPlayer();

        // 画传送装置方位箭头
        DrawPassagesRangeArrows();

        // 画操作按钮
        HandleInputEvent();
    }

    private void DrawFloorMap()
    {
        if (!mapService.IsMapReadyToDraw())
            return;
        var info = mapService.CurrentMap.Info;
        radarWindow.AddImageQuad(mapService.ColoredMapTexture.Handle,
            info.TopLeft.Transform(worldToRadarMatrix),
            info.TopRight.Transform(worldToRadarMatrix),
            info.BottomRight.Transform(worldToRadarMatrix),
            info.BottomLeft.Transform(worldToRadarMatrix)
        );

        HomeFlagTex.DrawOnWindow(
            radarWindow,
            deepDungeonService.LandingPosition.Transform(worldToRadarMatrix));
    }

    private void DrawRadarObjects()
    {
        foreach (var radarObj in RadarObjList)
        {
            var pos = radarObj.Position.Transform(worldToRadarMatrix);
            if (radarObj.ShowName())
                radarWindow.DrawDotWithText(
                    pos,
                    radarObj.GetDisplayName(),
                    radarObj.GetMarkerConfig().Color);
            else
            {
                switch (radarObj.Kind)
                {
                    case RadarObject.RadarObjectKind.Return:
                        (deepDungeonService.ReturnActivated ? Return10MiniIcon : Return0MiniIcon).DrawOnWindow(radarWindow, pos, config.IconScales.EventObj);
                        break;
                    case RadarObject.RadarObjectKind.Passage:
                        (deepDungeonService.PassageActivated ? Passage10MiniIcon : Passage0MiniIcon).DrawOnWindow(radarWindow, pos, config.IconScales.EventObj);
                        break;
                    case RadarObject.RadarObjectKind.Votive:
                        VotiveIcon.DrawOnWindow(radarWindow, pos, config.IconScales.EventObj);
                        break;
                    case RadarObject.RadarObjectKind.BronzeChest:
                        BronzeChestIcon.DrawOnWindow(radarWindow, pos, config.IconScales.Chest);
                        break;
                    case RadarObject.RadarObjectKind.SilverChest:
                        SilverChestIcon.DrawOnWindow(radarWindow, pos, config.IconScales.Chest);
                        break;
                    case RadarObject.RadarObjectKind.GoldChest:
                        GoldChestIcon.DrawOnWindow(radarWindow, pos, config.IconScales.Chest);
                        break;
                    case RadarObject.RadarObjectKind.AccursedHoard:
                        TreasureGlowTex.DrawOnWindow(radarWindow, pos, config.IconScales.AccursedHoard, (Environment.TickCount % 1000) / 1000f * MathF.Tau);
                        AccursedHoardTex.DrawOnWindow(radarWindow, pos, config.IconScales.AccursedHoard);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private void DrawLocalPlayer()
    {
        radarWindow.PathArcTo(meRadarPos, radarScale * 25f, -radarRotation - cameraRotation - MathF.PI * 0.75f, -radarRotation - cameraRotation - MathF.PI * 0.25f);
        radarWindow.PathLineTo(meRadarPos);
        radarWindow.PathStroke(config.Markers.Player.Color, ImDrawFlags.Closed, 1f);
        //FovConeTex.DrawOnRadar(radarWindow, meRadarPos, Vector2.One, radarRotation + cameraRotation + 0.25f * MathF.PI);

        if (config.RadarObjectUseIcons)
        {
            PlayerArrowTex.DrawOnWindow(radarWindow, meRadarPos, config.IconScales.LocalPlayer, playerRotation - MathF.PI + radarRotation);
        }
        else
        {
            radarWindow.DrawDotWithText(meRadarPos, "我", config.Markers.Player.Color);
        }

        // 画辅助圈
        radarWindow.AddCircle(meRadarPos, radarScale * 80f, config.RadarSenseCircleOutlineColor);
    }

    private void DrawPassagesRangeArrows()
    {
        foreach (var passagePos in PassageMarkers)
        {
            var offset = passagePos - meWorldPos;
            var distance = offset.Length();
            if (distance <= 80f)
                continue;
            var arrowRotation = Angle.FromDirection(offset).Rad;
            var arrowWorldPos = meWorldPos + 80f * offset / distance; // 画在圈上
            ArrowIcon.DrawOnWindow(
                radarWindow,
                arrowWorldPos.Transform(worldToRadarMatrix),
                new Vector2(config.IconScales.PassageArrow),
                arrowRotation + radarRotation,
                config.Markers.EventObj.Color);
        }
    }

    private void HandleInputEvent()
    {
        if (!config.RadarClickThrough)
        {
            ImGui.SetCursorPos(ImGuiHelpers.ScaledVector2(5f, 5f));
            var iconSize = ImGuiHelpers.ScaledVector2(25f, 25f);
            var icon = config.RadarOrientationFixed ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow;
            if (ImGuiUtils.IconButton(icon, "ToggleSnap", iconSize))
            {
                config.RadarOrientationFixed ^= true;
            }
            ImGui.SetCursorPosX(5f);
            if (ImGuiUtils.IconButton(FontAwesomeIcon.PlusCircle, "zoom++", iconSize))
            {
                Zoom += 0.1f;
            }
            ImGui.SetCursorPosX(5f);
            if (ImGuiUtils.IconButton(FontAwesomeIcon.MinusCircle, "zoom--", iconSize))
            {
                Zoom -= 0.1f;
            }
            if (ImGui.IsWindowHovered())
            {
                Zoom += ImGui.GetIO().MouseWheel * 0.1f;
            }
        }
    }
}
