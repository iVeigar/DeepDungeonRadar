using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Network;
using DeepDungeonRadar.Maps;
using DeepDungeonRadar.util;
using FFXIVClientStructs.FFXIV.Component.GUI;
using static DeepDungeonRadar.util.DeepDungeonUtil;

namespace DeepDungeonRadar.Services;

public partial class DeepDungeonService : IDisposable
{
    [GeneratedRegex("\\d+")]
    private static partial Regex FloorNumber();
    private readonly Configuration config = Service.Config;
    private readonly MapDrawer mapDrawer;
    private bool FloorVerified = false;
    private int _currentFloor = 0;
    private unsafe int CurrentFloor
    {
        get
        {
            if (!FloorVerified)
                VerifyFloorNumber();
            return _currentFloor;
        }
    }
    private bool _floorTransfer = true; 
    public bool FloorTransfer
    {
        get => _floorTransfer;
        private set
        {
            if (_floorTransfer != value)
            {
                _floorTransfer = value;
                if (value)
                {
                    Service.Log.Debug($"Teleporting..");
                    mapDrawer.ResetColliderBox();
                }
                else
                {
                    _currentFloor++;
                    mapDrawer.Update();
                    Service.Log.Debug($"Entered new floor #{CurrentFloor}{(FloorVerified ? "" : " (not verified)")}");
                }
            }
        }
    }

    public bool HasRadar => !(CurrentFloor % 10 == 0 || (InEO && CurrentFloor == 99));

    public DeepDungeonService(MapDrawer mapDrawer)
    {
        this.mapDrawer = mapDrawer;
        Service.GameNetwork.NetworkMessage += NetworkMessage;
        OnConditionChange(ConditionFlag.BetweenAreas, false); // manually trigger to initialize
        Service.Condition.ConditionChange += OnConditionChange;
    }

    private unsafe void VerifyFloorNumber()
    {
        var a = Service.GameGui.GetAddonByName("DeepDungeonMap", 1);
        if (a == IntPtr.Zero)
            return;
        var addon = (AtkUnitBase*)a;
        var floorText = addon->GetNodeById(17)->ChildNode->PrevSiblingNode->GetAsAtkTextNode()->NodeText.ToString();
        var floor = int.Parse(FloorNumber().Match(floorText).Value);
        if (_currentFloor != floor)
        {
            Service.Log.Debug($"Adjusted floor number to #{floor}");
            _currentFloor = floor;
        }
        FloorVerified = true;
    }

    public void OnConditionChange(ConditionFlag flag, bool value)
    {
        switch (flag)
        {
            case ConditionFlag.InDeepDungeon:
                if (!value)
                {
                    Service.Log.Debug($"Exited dungeon.");
                    mapDrawer.ClearMapData();
                    mapDrawer.ClearFloorDetailData();
                    _currentFloor = 0;
                    _floorTransfer = true; // don't want to trigger set action of property FloorTransfer
                    FloorVerified = false;
                }
                break;
            case ConditionFlag.BetweenAreas:
                if (!value && Service.Condition[ConditionFlag.InDeepDungeon])
                {
                    FloorTransfer = false;
                }
                break;
        }
    }

    public void Dispose()
    {
        Service.Condition.ConditionChange -= OnConditionChange;
        Service.GameNetwork.NetworkMessage -= NetworkMessage;
    }

    private void NetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
    {
        if (direction == NetworkMessageDirection.ZoneDown && opCode == (ushort)ServerZoneIpcType.SystemLogMessage)
        {
            var logId = (uint)ReadNumber(dataPtr, 4, 4);
            if (Service.Condition[ConditionFlag.InDeepDungeon] && logId == DataIds.SystemLogTransferenceInitiated)
                FloorTransfer = true;
        }
    }

    private static int ReadNumber(IntPtr dataPtr, int offset, int size)
    {
        var bytes = new byte[4];
        Marshal.Copy(dataPtr + offset, bytes, 0, size);
        return BitConverter.ToInt32(bytes);
    }
}
