using System;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Data;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DeepDungeonRadar.Radar;

public sealed partial class DeepDungeonService : IDisposable
{
    [GeneratedRegex("\\d+")]
    private static partial Regex AddonFloorNumber();

    [GeneratedRegex(@"(?:地下|第)(\d+)(?:层|朝圣路)")]
    private static partial Regex ChatMessageFloorNumber();

#pragma warning disable CS0649
    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7);
    [EzHook("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", nameof(ActorControlSelfDetour))]
    private readonly EzHook<ActorControlSelfDelegate> ActorControlSelfHook;

    private unsafe delegate void SystemLogMessageDelegate(uint entityId, uint logMessageId, int* args, byte argCount);
    [EzHook("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B6 47 28", nameof(SystemLogMessageDetour))]
    private readonly EzHook<SystemLogMessageDelegate> SystemLogMessageHook;
#pragma warning restore CS0649

    public event Action? ExitingCurrentFloor;
    public event Action? EnteredNewFloor;
    public event Action? ExitedDeepDungeon;

    private readonly Configuration config = Plugin.Config;
    private int chatMessageFloor;
    private int addonFloor;
    private int startFloor;
    private long lastCheckAt;
    private bool inPotD;
    private bool inHoH;
    private bool inEO;
    private bool inPT;

    public static Vector3 MeWorldPos => Svc.ClientState.LocalPlayer?.Position ?? default;
    public static ushort CurrentTerritory => Svc.ClientState.TerritoryType;

    public int CurrentFloor
    {
        get
        {
            if (Environment.TickCount64 - lastCheckAt > 200)
            {
                lastCheckAt = Environment.TickCount64;
                int floor = chatMessageFloor != 0 ? chatMessageFloor : (TryGetAddonFloorNumber() ? addonFloor : startFloor);
                if (floor > field && !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.BetweenAreas51])
                {
                    field = floor;
                    Svc.Log.Debug($"Entered new floor: #{field}");
                    EnteredNewFloor?.Invoke();
                }
            }
            return field;
        }
        private set;
    }
    
    public bool HasRadar => CurrentFloor % 10 != 0 && (!(inEO || inPT) || CurrentFloor < 99);

    public bool FloorTransfer { get; private set; }
    
    public bool AccursedHoardOpened { get; private set; }
    
    public bool InDeepDungeon => inPotD || inHoH || inEO || inPT;

    public DeepDungeonService()
    {
        Svc.Condition.ConditionChange += OnConditionChange;
        Svc.Chat.CheckMessageHandled += OnChatMessage;
        ExitingCurrentFloor += OnExitingCurrentFloor;
        EnteredNewFloor += OnEnteredNewFloor;
        ExitedDeepDungeon += OnExitedDeepDungeon;
        RefreshInDungeon();
    }

    private void OnEnteredNewFloor()
    {
        AccursedHoardOpened = false;
        FloorTransfer = false;
    }

    private void RefreshInDungeon()
    {
        inPotD = CurrentTerritory.InPotD();
        inHoH = CurrentTerritory.InHoH();
        inEO = CurrentTerritory.InEO();
        inPT = CurrentTerritory.InPT();
    }
    private void OnExitingCurrentFloor()
    {
        FloorTransfer = true;
    }

    private void OnExitedDeepDungeon()
    {
        inPotD = inHoH = inEO = inPT = false;
        chatMessageFloor = 0;
        addonFloor = 0;
        startFloor = 0;
        CurrentFloor = 0;
        AccursedHoardOpened = false;
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.InDeepDungeon)
            return;
        if (value)
        {
            Svc.Log.Debug($"Entering deep dungeon");
            RefreshInDungeon();
        }
        else
        {
            Svc.Log.Debug($"Exited deep dungeon");
            ExitedDeepDungeon?.Invoke();
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString seMessage, ref bool isHandled)
    {
        if (((ushort)type & 0x7f) != (ushort)XivChatType.SystemMessage)
            return;

        var match = ChatMessageFloorNumber().Match(seMessage.ToString());
        if (match.Success)
            chatMessageFloor = int.Parse(match.Groups[1].Value);
    }

    private void ActorControlSelfDetour(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7)
    {
        ActorControlSelfHook!.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId, param7);

        if (eventId == NetworkIds.ActorControlSelfDirectorUpdate)
        {
            if ((param2 & 0xff) == NetworkIds.DirectorUpdateDutyCommenced)
            {
                var contentId = (int)param1 & 0xffff;
                if (startFloor == 0 && DeepDungeonContentInfo.ContentInfo.TryGetValue(contentId, out var info))
                    startFloor = info.StartFloor;
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
                Svc.Log.Debug("Floor transference initiated, exiting current floor.");
                ExitingCurrentFloor?.Invoke();
                break;
            case 7275:
            case 7276:
                AccursedHoardOpened = true;
                break;

        }
    }

    private unsafe bool TryGetAddonFloorNumber()
    {
        var addon = Svc.GameGui.GetAddonByName("DeepDungeonMap", 1);
        if (!addon.IsReady || !addon.IsVisible)
        {
            addonFloor = 0;
            return false;
        }
        var floorText = ((AtkUnitBase*)addon.Address)->GetNodeById(26)->ChildNode->PrevSiblingNode->GetAsAtkTextNode()->NodeText.ToString();
        addonFloor = int.Parse(AddonFloorNumber().Match(floorText).Value);
        return true;
    }

    public void Dispose()
    {
        ExitingCurrentFloor -= OnExitingCurrentFloor;
        EnteredNewFloor -= OnEnteredNewFloor;
        ExitedDeepDungeon -= OnExitedDeepDungeon;

        Svc.Condition.ConditionChange -= OnConditionChange;
        Svc.Chat.CheckMessageHandled -= OnChatMessage;
    }
}
