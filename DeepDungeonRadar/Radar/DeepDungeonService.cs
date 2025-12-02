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

    // Svc.Condition变化的时间顺序
    // 1. 从外面进入深宫时 BetweenAreas -> true, InDeepDungeon -> true, BetweenAreas -> false(#1), 人物在圈圈里出现, Occupied33 -> true, Occupied33 -> false(#2), 使命开始. 在#1和#2处各检查一次
    // 2. 进入下一层时 Occupied33 -> true(#1), BetweenAreas -> true(#2), Occupied33 -> false(#3), BetweenAreas -> false(#4). 在#1处有系统提示“成功进行了传送”，且需要在#2之前还原碰撞盒所以不能在#2处检查；在#4后人物位置才更新，所以不要在#3处检查，而是在#4处检查
    // 3. 退出深宫时 BetweenAreas -> true, InDeepDungeon -> false, BetweenAreas -> false(#1). 在#1处检查出本即可
    public unsafe void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BetweenAreas && !value)
        {
            if (Svc.Condition[ConditionFlag.InDeepDungeon])
            {
                CheckEnteredNewFloor(flag);
            }
            else if (InDeepDungeon)
            {
                // 3#1
                InPotD = InHoH = InEO = InPT = AccursedHoardOpened = false;
                CurrentFloor = 0;
                FloorTransfer = true;
                Svc.Log.Debug($"Exited deep dungeon");
                ExitingCurrentFloor?.Invoke(true);
            }
        }
        if (flag == ConditionFlag.Occupied33 && !value)
        {
            if (Svc.Condition[ConditionFlag.InDeepDungeon])
            {
                CheckEnteredNewFloor(flag);
            }
        }
    } 

    private unsafe void CheckEnteredNewFloor(ConditionFlag flag)
    {
        var dd = EventFramework.Instance()->GetInstanceContentDeepDungeon();
        if (dd == null || dd->Floor == 0 || dd->ContentId == 60052)
            return;

        bool trigger = false;
        // 1#1, 1#2
        if (!InDeepDungeon)
        {
            InPotD = dd->DeepDungeonId == 1;
            InHoH = dd->DeepDungeonId == 2;
            InEO = dd->DeepDungeonId == 3;
            InPT = dd->DeepDungeonId == 4;
            trigger = true;
        }
        // 2#4
        else if (flag == ConditionFlag.BetweenAreas)
        {
            trigger = true;
        }

        if (trigger)
        {
            FloorTransfer = false;
            AccursedHoardOpened = false;
            CurrentFloor = dd->Floor;
            Svc.Log.Debug($"Entered new floor #{CurrentFloor}");
            EnteredNewFloor?.Invoke();
        }
    }

    private unsafe void SystemLogMessageDetour(uint entityId, uint logId, int* args, byte argCount)
    {
        SystemLogMessageHook!.Original(entityId, logId, args, argCount);
        if (!InDeepDungeon)
            return;

        switch (logId)
        {
            case 7248: // 2#1
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
