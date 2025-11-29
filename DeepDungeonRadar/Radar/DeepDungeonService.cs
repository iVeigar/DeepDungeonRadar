using System;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Data;
using ECommons;
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

    // false: only floor transfer, true: exit dungeon as well
    public delegate void ExitingCurrentFloorDelegate(bool exitDungeon);
    public event ExitingCurrentFloorDelegate? ExitingCurrentFloor;
    public event Action? EnteredNewFloor;

    private readonly Configuration config = Plugin.Config;
    private int chatMessageFloor;
    private int addonFloor;
    private int startFloor;
    private bool inPotD;
    private bool inHoH;
    private bool inEO;
    private bool inPT;

    public static Vector3 MeWorldPos => Svc.ClientState.LocalPlayer?.Position ?? default;
    public ushort CurrentTerritory { get; private set; }

    public int CurrentFloor { get; private set; }

    public bool HasRadar => InDeepDungeon && !FloorTransfer && HasMap;

    public bool HasMap => CurrentFloor % 10 != 0 && (!(inEO || inPT) || CurrentFloor < 99);

    private bool _floorTransfer = true;
    
    public bool FloorTransfer
    {   
        get => _floorTransfer;
        private set
        {
            if (_floorTransfer != value)
            {
                _floorTransfer = value;
                if (_floorTransfer)
                {
                    Svc.Log.Debug("Exiting current floor..");
                    ExitingCurrentFloor?.Invoke(false);
                }
                else
                {
                    Svc.Log.Debug($"Entered new floor #{CurrentFloor}");
                    AccursedHoardOpened = false;
                    EnteredNewFloor?.Invoke();
                }
            }
        }
    }
    
    public bool AccursedHoardOpened { get; private set; }
    
    public bool InDeepDungeon => inPotD || inHoH || inEO || inPT;

    public DeepDungeonService()
    {
        EzSignatureHelper.Initialize(this);
        Svc.Chat.CheckMessageHandled += OnChatMessage;
        Svc.ClientState.TerritoryChanged += OnTerritoryChange;
        Svc.Framework.Update += UpdateFloor;
    }

    private void UpdateFloor(IFramework _)
    {
        if (!InDeepDungeon || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
            return;
        var floor = chatMessageFloor != 0 ? chatMessageFloor : (TryGetAddonFloorNumber() ? addonFloor : startFloor);
        if (floor > CurrentFloor)
        {
            CurrentFloor = floor;
            FloorTransfer = false;
        }
    }

    public void OnTerritoryChange(ushort newTerritory)
    {
        inPotD = newTerritory.InPotD();
        inHoH = newTerritory.InHoH();
        inEO = newTerritory.InEO();
        inPT = newTerritory.InPT();
        CurrentTerritory = newTerritory;
        if (!InDeepDungeon)
        {
            Svc.Log.Debug($"Exited deep dungeon");
            chatMessageFloor = 0;
            addonFloor = 0;
            startFloor = 0;
            CurrentFloor = 0;

            AccursedHoardOpened = false;
            _floorTransfer = true;
            ExitingCurrentFloor?.Invoke(true);
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
                FloorTransfer = true;
                break;
            case 7275:
            case 7276:
                AccursedHoardOpened = true;
                break;

        }
    }

    private unsafe bool TryGetAddonFloorNumber()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("DeepDungeonMap", out var addon)
            && GenericHelpers.IsAddonReady(addon))
        {
            var floorText = addon->GetNodeById(26)->ChildNode->PrevSiblingNode->GetAsAtkTextNode()->NodeText.ToString();
            addonFloor = int.Parse(AddonFloorNumber().Match(floorText).Value);
            return true;
        }
        addonFloor = 0;
        return false;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= UpdateFloor;
        Svc.ClientState.TerritoryChanged -= OnTerritoryChange;
        Svc.Chat.CheckMessageHandled -= OnChatMessage;
    }
}
