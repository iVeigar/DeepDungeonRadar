using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Network;
using Dalamud.Logging;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.Extensions;
using DeepDungeonRadar.UI;
using DeepDungeonRadar.util;
using ImGuiNET;

namespace DeepDungeonRadar.Services;

// TnT = Trap and Treasure
public sealed class TnTService : IDisposable
{
    private readonly Configuration config;
    private readonly HashSet<Vector2> HoardBlackList = new();
    private readonly HashSet<Vector2> TrapBlacklist = new();

    public static Vector3 MeWorldPos { get; private set; } = Vector3.Zero;

    private bool NextFloorTransfer = false;
    private DeepDungeonTrapStatus TrapStatus = DeepDungeonTrapStatus.Active;
    private bool AccursedHoardCofferDiscovered = false;
    private bool FortuneActivated = false;
    private bool NextFloorFortune = false;

    private static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; } = new();

    public TnTService()
    {
        config = PluginService.Config;
        PluginService.PluginInterface.UiBuilder.Draw += OnUpdate;
        PluginService.GameNetwork.NetworkMessage += NetworkMessage;
    }

    public void Dispose()
    {
        PluginService.GameNetwork.NetworkMessage -= NetworkMessage;
        PluginService.PluginInterface.UiBuilder.Draw -= OnUpdate;
        PluginLog.Information($"TnT Service unloaded");
    }

    public void OnUpdate()
    {
        if (PluginService.ObjectTable == null) return;
        if (!InDeepDungeon()) return;
        EnumerateAllObjects();
        if (config.DeepDungeon_EnableTrapView)
        {
            DrawDeepDungeonObjects();
        }
    }

    private void EnumerateAllObjects()
    {
        var first = true;
        foreach (var o in PluginService.ObjectTable)
        {
            if (first)
            {
                first = false;
                MeWorldPos = o.Position;
                continue;
            }
            var location = o.Location2D();
            if (o.IsSilverCoffer())
            {
                TrapBlacklist.Add(location); // TODO 测试银箱子炸弹到底算不算触发了的陷阱（6388）//不管怎样都不计入陷阱地点记录
            }
            else if (o.IsAccursedHoard())
            {
                if (o.DataId == 2007543U && !AccursedHoardCofferDiscovered)
                {
                    AccursedHoardCofferDiscovered = true;
                    NextFloorFortune = false;
                }
                if (!HoardBlackList.Contains(location))
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

        var trapGrouping = (from i in config.DeepDungeonObjects
                            where i.Type == DeepDungeonObjectType.Trap && i.Territory != 0 && i.Bg == Util.TerritoryToBg(PluginService.ClientState.TerritoryType) && i.Location.Distance2D(meWorldPos) < config.DeepDungeon_ObjectShowDistance
                            select i).GroupBy(i => i, DeepDungeonObjectLocationEqual);
        var accursedHoardGrouping = (from i in config.DeepDungeonObjects
                            where i.Type == DeepDungeonObjectType.AccursedHoard && i.Territory != 0 && i.Bg == Util.TerritoryToBg(PluginService.ClientState.TerritoryType) && i.Location.Distance2D(meWorldPos) < config.DeepDungeon_ObjectShowDistance
                            select i).GroupBy(i => i, DeepDungeonObjectLocationEqual);

        if (TrapStatus == DeepDungeonTrapStatus.Active)
        {
            radius = 1.7f;
            color = Color.Red;
            foreach (var grouping in trapGrouping)
            {
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

        if (!FortuneActivated && !AccursedHoardCofferDiscovered)
        {
            radius = 2.0f;
            color = Color.Yellow;
            foreach (var grouping in accursedHoardGrouping)
            {
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
    
    private void EnterDeepDungeon()
    {
        NextFloorTransfer = true;
        NextFloorFortune = false;
        NextFloor();
    }

    private void NextFloor()
    {
        if (NextFloorTransfer)
        {
            PluginService.ChatGui.Print("[DDR] 进入下一层"); // TODO remove this Debug info
            NextFloorTransfer = false;
            TrapBlacklist.Clear();
            HoardBlackList.Clear();
            FortuneActivated = NextFloorFortune;
            TrapStatus = DeepDungeonTrapStatus.Active;
        }
    }

    private void ExitDeepDungeon()
    {
        NextFloorTransfer = false;
        TrapBlacklist.Clear();
        HoardBlackList.Clear();
    }

    public static bool InDeepDungeon()
    {
        var mapId = PluginService.ClientState.TerritoryType;
        return DataIds.PalaceOfTheDeadMapIds.Contains(mapId) || DataIds.HeavenOnHighMapIds.Contains(mapId) ||
               DataIds.EurekaOrthosMapIds.Contains(mapId);
    }

    private void NetworkMessage(
        IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
    {
        if (direction == NetworkMessageDirection.ZoneDown)
        {
            switch (opCode)
            {
                case (int)ServerZoneIpcType.SystemLogMessage:
                    OnSystemLogMessage(dataPtr, ReadNumber(dataPtr, 4, 4));
                    break;
                case (int)ServerZoneIpcType.ActorControlSelf:
                    OnActorControlSelf(dataPtr);
                    break;
            }
        }
    }

    private void OnActorControlSelf(IntPtr dataPtr)
    {
        // OnDirectorUpdate
        if (Marshal.ReadByte(dataPtr) == DataIds.ActorControlSelfDirectorUpdate)
        {
            switch (Marshal.ReadByte(dataPtr, 8))
            {
                // OnDutyCommenced
                case DataIds.DirectorUpdateDutyCommenced:
                {
                    var contentId = ReadNumber(dataPtr, 4, 2);
                    if (DeepDungeonContentInfo.ContentInfo.TryGetValue(contentId, out var _))
                        EnterDeepDungeon();
                    break;
                }
                // OnDutyRecommenced
                case DataIds.DirectorUpdateDutyRecommenced:
                    if (NextFloorTransfer)
                        NextFloor();
                    break;
            }
        }
    }

    private void OnSystemLogMessage(IntPtr dataPtr, int logId)
    {
        if (InDeepDungeon())
        {
            if (logId == DataIds.SystemLogPomanderUsed)
                OnPomanderUsed((Pomander)Marshal.ReadByte(dataPtr, 16));
            else if (logId == DataIds.SystemLogDutyEnded)
                ExitDeepDungeon();
            else if (logId == DataIds.SystemLogTransferenceInitiated)
                NextFloorTransfer = true;
        }
    }

    private void OnPomanderUsed(Pomander pomander)
    {
        PluginLog.Debug($"Pomander ID: {pomander}");
        switch (pomander)
        {
            case Pomander.Safety:
            case Pomander.SafetyProtomander:
                TrapStatus = DeepDungeonTrapStatus.Inactive;
                break;

            case Pomander.Sight:
            case Pomander.SightProtomander:
                if (TrapStatus == DeepDungeonTrapStatus.Active)
                    TrapStatus = DeepDungeonTrapStatus.Visible;
                break;

            case Pomander.Fortune:
            case Pomander.FortuneProtomander:
                FortuneActivated = true;
                NextFloorFortune = true;
                break;
        }
    }

    private static int ReadNumber(IntPtr dataPtr, int offset, int size)
    {
        var bytes = new byte[4];
        Marshal.Copy(dataPtr + offset, bytes, 0, size);
        return BitConverter.ToInt32(bytes);
    }
}
