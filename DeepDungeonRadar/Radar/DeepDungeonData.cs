using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using DeepDungeonRadar.Utils;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace DeepDungeonRadar.Radar;
public enum DeepDungeonBg
{
    none,
    f1c1,
    f1c2,
    f1c3,
    f1c4,
    f1c5,
    f1c6,
    f1c8,
    f1c9,
    f1c7,
    e3c1,
    e3c2,
    e3c3,
    e3c4,
    e3c5,
    e3c6,
    l5c1,
    l5c2,
    l5c3,
    l5c4,
    l5c5,
    l5c6,
    n6c1,
    n6c2,
    n6c3,
    n6c4,
    n6c5,
    n6c6,
}
public static class DeepDungeonData
{
    public const uint SilverChestId = 2007357;
    public const uint GoldChestId = 2007358;
    public const uint MimicChestId = 2006020;

    public const uint AccursedHoardId = 2007542;
    public const uint AccursedHoardCofferId = 2007543;
    public const uint TriggeredTrapId = 6388;
    // Pilgrimage's Traverse Candle Buffs
    public const uint VotiveId = 2014759;

    public static readonly HashSet<uint> IgnoredIds =
    [
        6388, // Trap object at <0, 0>, or invisible triggered trap
    ];

    public static readonly HashSet<uint> MimicNameIds =
    [
        2566, 6880, 7392, 7393, 7394, 14264, 14265, 14266
    ];

    public static readonly HashSet<uint> KorriganNameIds =
    [
        5041, // 皮古迈欧
        7610, // 柯瑞甘
        10309, // 正统柯瑞甘
        14267, // 交错路柯瑞甘
    ];

    public static readonly HashSet<uint> BronzeChestIds =
    [
        // PotD
        782, 783, 784, 785, 786, 787, 788, 789, 790, 802, 803, 804, 805,
        // HoH
        1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049,
        // EO
        1541, 1542, 1543, 1544, 1545, 1546, 1547, 1548, 1549, 1550, 1551, 1552, 1553, 1554,
        // PT
        1881, 1882, 1883, 1884, 1885, 1886, 1887, 1888, 1889, 1890, 1891, 1892, 1893, 1906, 1907, 1908,
    ];

    public static readonly Dictionary<uint, string> TrapIds = new()
    {
        { 2007182, "爆炸陷阱" },
        { 2007183, "诱饵陷阱" },
        { 2007184, "弱化陷阱" },
        { 2007185, "妨碍陷阱" },
        { 2007186, "蛙变陷阱" },
        { 2009504, "獭獭陷阱" },
        { 2013284, "猫头小鹰陷阱" },
        { 2014939, "妖灵陷阱" },
    };

    public static readonly HashSet<uint> PassageIds =
    [
        2007188, // PotD
        2009507, // HoH
        2013287, // EO
        2014756  // PT
    ];

    public static readonly HashSet<uint> ReturnIds =
    [
        2007187, // PotD
        2009506, // HoH
        2013286,  // EO
        2014755
    ];

    public static readonly HashSet<uint> HelpfulNpcNameIds =
    [
        // HoH
        7396, 7397, 7398,
    ];

    public static readonly HashSet<uint> PotDTerritoryIds =
    [
        561, 562, 563, 564, 565, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607
    ];

    public static readonly HashSet<uint> HoHTerritoryIds =
    [
        770, 771, 772, 782, 773, 783, 774, 784, 775, 785
    ];

    public static readonly HashSet<uint> EOTerritoryIds =
    [
        1099, 1100, 1101, 1102, 1103, 1104, 1105, 1106, 1107, 1108
    ];

    public static readonly HashSet<uint> PTTerritoryIds =
    [
        1281, 1282, 1283, 1284, 1285, 1286, 1287, 1288, 1289, 1290
    ];

    public static unsafe bool IsChestOpenedOrFaded(this IGameObject obj)
    {
        var chest = (FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure*)obj.Address;
        return chest->Flags != FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure.TreasureFlags.None;
    }

    public static bool IsIgnored(this IGameObject obj) => IgnoredIds.Contains(obj.BaseId);
    public static bool IsPlayer(this IGameObject obj) => obj.ObjectKind == ObjectKind.Player;

    public static bool IsMob(this IGameObject obj, out IBattleNpc bNpc)
    {
        if (obj.ObjectKind == ObjectKind.BattleNpc && (BattleNpcSubKind)obj.SubKind == BattleNpcSubKind.Enemy)
        {
            bNpc = (IBattleNpc)obj;
            return true;
        }
        bNpc = null;
        return false;
    }

    public static bool IsMimic(this IBattleNpc bNpc) => MimicNameIds.Contains(bNpc.NameId);

    public static bool IsKerrigan(this IBattleNpc bNpc) => KorriganNameIds.Contains(bNpc.NameId);

    public static bool IsHelpfulNpc(this IBattleNpc bNpc) => HelpfulNpcNameIds.Contains(bNpc.NameId);

    public static bool IsFriendly(this IBattleNpc bNpc) => bNpc.IsHelpfulNpc() || bNpc.IsKerrigan();

    public static bool IsPassage(this IGameObject obj) => PassageIds.Contains(obj.BaseId);

    public static bool IsReturn(this IGameObject obj) => ReturnIds.Contains(obj.BaseId);

    public static bool IsVotive(this IGameObject obj) => VotiveId == obj.BaseId;

    public static bool IsBronzeChest(this IGameObject obj) => BronzeChestIds.Contains(obj.BaseId);

    public static bool IsSilverChest(this IGameObject obj) => SilverChestId == obj.BaseId;

    public static bool IsGoldChest(this IGameObject obj) => GoldChestId == obj.BaseId;

    public static bool IsAccursedHoard(this IGameObject obj) => AccursedHoardId == obj.BaseId || AccursedHoardCofferId == obj.BaseId;

    public static bool IsMimicChest(this IGameObject obj) => MimicChestId == obj.BaseId;

    public static Vector2 Position2D(this IGameObject obj) => obj.Position.ToVector2();

    public static DeepDungeonBg ToBg(this uint territory) => Enum.TryParse<DeepDungeonBg>(Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territory).Bg.ToString().Split('/')[^1], true, out var bg) ? bg : DeepDungeonBg.none;

    public static bool InPotD(this uint territory) => PotDTerritoryIds.Contains(territory);

    public static bool InHoH(this uint territory) => HoHTerritoryIds.Contains(territory);

    public static bool InEO(this uint territory) => EOTerritoryIds.Contains(territory);

    public static bool InPT(this uint territory) => PTTerritoryIds.Contains(territory);
}
