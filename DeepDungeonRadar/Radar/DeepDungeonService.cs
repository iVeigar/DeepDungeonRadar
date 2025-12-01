using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using RoomFlags = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentDeepDungeon.RoomFlags;
namespace DeepDungeonRadar.Radar;

public sealed partial class DeepDungeonService : IDisposable
{
#pragma warning disable CS0649
    private unsafe delegate void SystemLogMessageDelegate(uint entityId, uint logMessageId, int* args, byte argCount);
    [EzHook("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B6 47 28", nameof(SystemLogMessageDetour))]
    private readonly EzHook<SystemLogMessageDelegate> SystemLogMessageHook;
#pragma warning restore CS0649
    // false: only floor transfer, true: exit dungeon as well
    public delegate void ExitingCurrentFloorDelegate(bool exitDungeon);
    public event ExitingCurrentFloorDelegate? ExitingCurrentFloor;
    public event Action? EnteredNewFloor;
    public bool InDeepDungeon => InPotD || InHoH || InEO || InPT;
    public bool HasMap => CurrentFloor % 10 != 0 && (!(InEO || InPT) || CurrentFloor < 99);
    public bool IsRadarReady => InDeepDungeon && !FloorTransfer && HasMap;
    public bool InPotD { get; private set; }
    public bool InHoH { get; private set; }
    public bool InEO { get; private set; }
    public bool InPT { get; private set; }
    public int CurrentFloor { get; private set; }
    public bool AccursedHoardOpened { get; private set; }
    public bool FloorTransfer { get; private set; } = true;
    public DeepDungeonService()
    {
        EzSignatureHelper.Initialize(this);
        Svc.Framework.RunOnFrameworkThread(() => OnConditionChanged(ConditionFlag.BetweenAreas, Svc.Condition[ConditionFlag.BetweenAreas]));
        Svc.Condition.ConditionChange += OnConditionChanged;
    }

    // 从外面进入深宫时 BetweenAreas -> true, InDeepDungeon -> true, BetweenAreas -> false
    // 进入下一层时 BetweenAreas -> true, Occupied33 -> false, BetweenAreas -> false
    // 退出深宫时 BetweenAreas -> true, InDeepDungeon -> false, BetweenAreas -> false
    public unsafe void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BetweenAreas && !value)
        {
            if (Svc.Condition[ConditionFlag.InDeepDungeon])
            {
                var dd = EventFramework.Instance()->GetInstanceContentDeepDungeon();
                if (dd != null && dd->ContentId != 60052)
                {
                    if (!InDeepDungeon)
                    {
                        InPotD = dd->DeepDungeonId == 1;
                        InHoH = dd->DeepDungeonId == 2;
                        InEO = dd->DeepDungeonId == 3;
                        InPT = dd->DeepDungeonId == 4;
                    }
                    FloorTransfer = false;
                    AccursedHoardOpened = false;
                    CurrentFloor = dd->Floor;
                    Svc.Log.Debug($"Entered new floor #{CurrentFloor}");
                    EnteredNewFloor?.Invoke();
                }
            }
            else if (InDeepDungeon)
            {
                InPotD = InHoH = InEO = InPT = AccursedHoardOpened = false;
                CurrentFloor = 0;
                FloorTransfer = true;
                Svc.Log.Debug($"Exited deep dungeon");
                ExitingCurrentFloor?.Invoke(true);
            }
        }
    } 

    private unsafe void SystemLogMessageDetour(uint entityId, uint logId, int* args, byte argCount)
    {
        SystemLogMessageHook!.Original(entityId, logId, args, argCount);
        if (!InDeepDungeon)
            return;

        switch (logId)
        {
            case 7248:
                Svc.Log.Debug("Exiting current floor..");
                FloorTransfer = true;
                ExitingCurrentFloor?.Invoke(false);
                break;
            case 7275:
            case 7276:
                AccursedHoardOpened = true;
                break;
        }
    }

    public unsafe List<int> GetPassageRooms()
    {
        var rooms = new List<int>();
        var dd = EventFramework.Instance()->GetInstanceContentDeepDungeon();
        if (dd == null)
            return rooms;
        for (int i = 0; i < 25; i++)
        {
            if (dd->MapData[i].HasFlag(RoomFlags.Passage))
            {
                rooms.Add(i);
            }
        }
        return rooms;
    }

    public void Dispose()
    {
        Svc.Condition.ConditionChange -= OnConditionChanged;
    }
}
