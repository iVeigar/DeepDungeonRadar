using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using RoomFlags = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentDeepDungeon.RoomFlags;
using ECommons.MathHelpers;
using ECommons.Logging;
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
    public bool ReturnActivated { get; private set; }
    public bool PassageActivated { get; private set; }
    public Vector2 LandingPosition { get; private set; }
    public DeepDungeonService()
    {
        EzSignatureHelper.Initialize(this);
        Svc.Framework.RunOnFrameworkThread(() => OnConditionChanged(ConditionFlag.BetweenAreas, Svc.Condition[ConditionFlag.BetweenAreas]));
        Svc.Condition.ConditionChange += OnConditionChanged;
    }

    // Svc.Condition变化的时间顺序和各事件调用规则
    //
    // 1. 从外面进入深宫(此时this.InDeepDungeon=false): [BetweenAreas]=true -> [InDeepDungeon]=true -> [BetweenAreas]=false(#1) -> 人物在圈圈里出现 -> [Occupied33]=true -> [Occupied33]=false(#2) -> 使命开始
    //      在#1和#2处各调用一次TryInvokeEnteredNewFloor() -> this.InDeepDungeon=true
    //
    // 2. 进入下一层时(此时this.InDeepDungeon=true): [Occupied33]=true(#1) -> [BetweenAreas]=true(#2) -> [Occupied33]=false(#3) -> [BetweenAreas]=false(#4)
    //      在#1处有LogMessage"成功进行了传送"，且#2处无法还原碰撞盒，所以在#1处调用ExitingCurrentFloor(false)
    //      在#4后人物位置才更新，所以在#4处调用TryInvokeEnteredNewFloor()
    //
    // 3. 退出深宫时(此时this.InDeepDungeon=true): [BetweenAreas]=true -> [InDeepDungeon]=false(#1) -> [BetweenAreas]=false(#2)
    //      在#1处调用ExitingCurrentFloor(true) -> this.InDeepDungeon=false
    public unsafe void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (value) return;

        if (flag == ConditionFlag.BetweenAreas)
        {
            // 1#1, 2#4
            TryInvokeEnteredNewFloor();
        }
        else if (flag == ConditionFlag.Occupied33)
        {
            // 1#2, no 2#3
            if (!InDeepDungeon) 
                TryInvokeEnteredNewFloor();
        }
        else if (flag == ConditionFlag.InDeepDungeon)
        {
            // 3#1
            if (InDeepDungeon)
            {
                InPotD = InHoH = InEO = InPT = AccursedHoardOpened = ReturnActivated = PassageActivated = false;
                CurrentFloor = 0;
                FloorTransfer = true;
                PluginLog.Debug($"Exited deep dungeon");
                ExitingCurrentFloor?.Invoke(true);
            }
        }
    }

    private unsafe void TryInvokeEnteredNewFloor()
    {
        if (!Svc.Condition[ConditionFlag.InDeepDungeon]) 
            return;
        var dd = EventFramework.Instance()->GetInstanceContentDeepDungeon();
        if (dd == null || dd->Floor == 0 || dd->ContentId == 60052)
            return;

        if (!InDeepDungeon)
        {
            InPotD = dd->DeepDungeonId == 1;
            InHoH = dd->DeepDungeonId == 2;
            InEO = dd->DeepDungeonId == 3;
            InPT = dd->DeepDungeonId == 4;
        }
        FloorTransfer = AccursedHoardOpened = ReturnActivated = PassageActivated = false;
        CurrentFloor = dd->Floor;
        LandingPosition = Player.Position.ToVector2();
        PluginLog.Debug($"Entered new floor #{CurrentFloor}");
        EnteredNewFloor?.Invoke();
    }

    private unsafe void SystemLogMessageDetour(uint entityId, uint logId, int* args, byte argCount)
    {
        SystemLogMessageHook!.Original(entityId, logId, args, argCount);
        if (!InDeepDungeon)
            return;

        switch (logId)
        {
            case 7245: // xxxx启动了！
                var dataId = (uint)args[0];
                if (DeepDungeonData.ReturnIds.Contains(dataId))
                    ReturnActivated = true;
                else if (DeepDungeonData.PassageIds.Contains(dataId))
                    PassageActivated = true;
                break;
            case 7248: // 2#1
                PluginLog.Debug("Exiting current floor..");
                FloorTransfer = true;
                ExitingCurrentFloor?.Invoke(false);
                break;
            case 7275:
            case 7276:
                AccursedHoardOpened = true;
                break;
            case 9208: // DeepDungeonMagicStone // todo 用了魔石后没有7245消息，假设两个装置会全都开
                ReturnActivated = PassageActivated = true;
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
