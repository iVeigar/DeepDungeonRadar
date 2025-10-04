using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DeepDungeonRadar.Maps;
using DeepDungeonRadar.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVOpcodes.CN;
using static DeepDungeonRadar.Misc.DeepDungeonUtil;

namespace DeepDungeonRadar.Services;

public sealed partial class DeepDungeonService : IDisposable
{
    [GeneratedRegex("\\d+")]
    private static partial Regex AddonFloorNumber();

    [GeneratedRegex(@"(?:地下|第)(\d+)层")]
    private static partial Regex SystemLogFloorNumber();

    private const string ActorControlSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";
    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7);
    private Hook<ActorControlSelfDelegate>? actorControlSelfHook;


    private const string SystemLogSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B6 47 28";
    private Hook<SystemLogMessageDelegate>? systemLogMessageHook;
    private unsafe delegate void SystemLogMessageDelegate(uint entityId, uint logMessageId, int* args, byte argCount);


    private readonly Configuration config = Service.Config;
    private readonly MapDrawer mapDrawer;
    private int startFloor = 0;
    private int systemLogFloor = 0;
    private int currentFloor = 0;
    private long lastCheckAt = Environment.TickCount64;
    private unsafe int CurrentFloor
    {
        get
        {
            if (Environment.TickCount64 - lastCheckAt > 500)
            {
                lastCheckAt = Environment.TickCount64;
                int floor = systemLogFloor != 0 ? systemLogFloor : (TryGetAddonFloorNumber(out var addonFloor) ? addonFloor : startFloor);
                if (floor > currentFloor)
                {
                    Service.Log.Debug($"Entered a new floor: #{floor}");
                    currentFloor = floor;
                    FloorTransfer = false;
                }
            }
            return currentFloor;
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
                if (value)
                    mapDrawer.ResetColliderBox();
                else
                    mapDrawer.Update();
                _floorTransfer = value;
            }
        }
    }

    public bool HasRadar => !(CurrentFloor % 10 == 0 || (InEO && CurrentFloor == 99));

    public DeepDungeonService(MapDrawer mapDrawer)
    {
        this.mapDrawer = mapDrawer;
        unsafe
        {
            var actorControlSelfPtr = Service.SigScanner.ScanText(ActorControlSig);
            actorControlSelfHook =
                Service.GameInteropProvider.HookFromAddress<ActorControlSelfDelegate>(actorControlSelfPtr, ActorControlSelf);
            actorControlSelfHook.Enable();

            var systemLogPtr = Service.SigScanner.ScanText(SystemLogSig);
            systemLogMessageHook =
                Service.GameInteropProvider.HookFromAddress<SystemLogMessageDelegate>(systemLogPtr, SystemLogMessage);
            systemLogMessageHook.Enable();
        }

        Service.Condition.ConditionChange += OnConditionChange;
        Service.ChatGui.CheckMessageHandled += OnChatMessage;
    }
    public void Dispose()
    {
        Service.Condition.ConditionChange -= OnConditionChange;
        Service.ChatGui.CheckMessageHandled -= OnChatMessage;
        actorControlSelfHook?.Disable();
        actorControlSelfHook?.Dispose();
        systemLogMessageHook?.Disable();
        systemLogMessageHook?.Dispose();
    }

    public void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.InDeepDungeon && !value)
        {
            Service.Log.Debug($"Exited dungeon.");
            mapDrawer.ClearMapData();
            mapDrawer.ClearFloorDetailData();
            startFloor = 0;
            systemLogFloor = 0;
            currentFloor = 0;
            mapDrawer.Cheated = false;
            _floorTransfer = true; // don't want to trigger set action of property FloorTransfer
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString seMessage, ref bool isHandled)
    {
        if (((ushort)type & 0x7f) != (ushort)XivChatType.SystemMessage)
            return;

        var match = SystemLogFloorNumber().Match(seMessage.ToString());
        if (match.Success)
            systemLogFloor = int.Parse(match.Groups[1].Value);
    }
    private void ActorControlSelf(uint category, uint eventId, uint param1, uint contentId, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7)
    {
        actorControlSelfHook!.Original(category, eventId, param1, contentId, param3, param4, param5, param6, targetId, param7);

        if (eventId == DataIds.ActorControlSelfDirectorUpdate && startFloor == 0 && DeepDungeonContentInfo.ContentInfo.TryGetValue((int)contentId, out var info))
            startFloor = info.StartFloor;
    }

    //private void OnActorControlSelf(IntPtr dataPtr)
    //{
    //    // OnDirectorUpdate
    //    if (Marshal.ReadByte(dataPtr) == DataIds.ActorControlSelfDirectorUpdate)
    //    {
    //        switch (Marshal.ReadByte(dataPtr, 8))
    //        {
    //            // OnDutyCommenced
    //            case DataIds.DirectorUpdateDutyCommenced:
    //                var contentId = ReadNumber(dataPtr, 4, 2);
    //                if (startFloor == 0 && DeepDungeonContentInfo.ContentInfo.TryGetValue(contentId, out var info))
    //                    startFloor = info.StartFloor;
    //                break;
    //        }
    //    }
    //}

    private unsafe void SystemLogMessage(uint entityId, uint logId, int* args, byte argCount)
    {
        systemLogMessageHook!.Original(entityId, logId, args, argCount);

        if (Service.Condition[ConditionFlag.InDeepDungeon])
        {
            if (logId == DataIds.SystemLogTransferenceInitiated)
            {
                FloorTransfer = true;
            }
        }
    }

    private static int ReadNumber(IntPtr dataPtr, int offset, int size)
    {
        var bytes = new byte[4];
        Marshal.Copy(dataPtr + offset, bytes, 0, size);
        return BitConverter.ToInt32(bytes);
    }

    private unsafe bool TryGetAddonFloorNumber(out int addonFloor)
    {
        var addon = Service.GameGui.GetAddonByName("DeepDungeonMap", 1);
        if (addon == IntPtr.Zero)
        {
            addonFloor = 0;
            return false;
        }
        var floorText = ((AtkUnitBase*)addon.Address)->GetNodeById(26)->ChildNode->PrevSiblingNode->GetAsAtkTextNode()->NodeText.ToString();
        addonFloor = int.Parse(AddonFloorNumber().Match(floorText).Value);
        return true;
    }
}
