using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using DeepDungeonRadar.Utils;

namespace DeepDungeonRadar.Data;

public static class DeepDungeonObject
{
    private static readonly Dictionary<uint, string> CustomNames = new()
    {
        { SilverChest, "（银）"},
        { GoldChest, "（金）"},
        { MimicChest, "（拟态怪）"},
        { AccursedHoard, "（宝藏）" },
        { AccursedHoardCoffer, "（宝藏）" },
        //{ 2007182, "爆炸陷阱" },
        //{ 2007183, "诱饵陷阱" },
        //{ 2007184, "弱化陷阱" },
        //{ 2007185, "妨碍陷阱" },
        //{ 2007186, "蛙变陷阱" },
        //{ 2009504, "獭獭陷阱" },
        //{ 2013284, "猫头小鹰陷阱" },
        //{ 2014939, "妖灵陷阱" },
    };

    public static string GetDisplayName(this IGameObject obj)
    {
        if (CustomNames.TryGetValue(obj.BaseId, out var value))
            return value;
        if (BronzeChestIDs.Contains(obj.BaseId))
            return "（铜）";
        var name = obj.Name.TextValue;
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (Plugin.Config.RemoveNamePrefix)
            {
                foreach (var prefix in Plugin.Config.NamePrefixes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (name.StartsWith(prefix))
                    {
                        name = name[prefix.Length..];
                        break;
                    }
                }
            }
            return name;
        }
        else
            return $"D:{obj.BaseId}" + (obj is IBattleNpc b ? $" N:{b.NameId}" : "");
    }

    public const uint SilverChest = 2007357;
    public const uint GoldChest = 2007358;
    public const uint MimicChest = 2006020;

    public const uint AccursedHoard = 2007542;
    public const uint AccursedHoardCoffer = 2007543;

    // Pilgrimage's Traverse Candle Buffs
    public const uint Votive = 2014759;


    public static readonly HashSet<uint> IgnoredDataIDs =
    [
        0,       // Players
        6388,    // Triggered Trap
        1023070, // ??? Object way out
        2000608, // ??? Object in Boss Room
        2005809, // Exit
        2001168, // Twistaaa

        // Random friendly stuff
        15898, 15899, 15860,
        18867, 18868, 18869, 
        10489, 16926, 7245,
        13961, 10487
    ];

    public static readonly HashSet<uint> MimicNameIDs =
    [
        2566, 6880, 7392, 7393, 7394, 14264, 14265, 14266
    ];

    public static readonly HashSet<uint> KorriganNameIDs =
    [
        5041, // 皮古迈欧
        7610, // 柯瑞甘
        10309, // 正统柯瑞甘
        14267, // 交错路柯瑞甘
    ];

    public static readonly HashSet<uint> BronzeChestIDs =
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

    public static readonly Dictionary<uint, string> TrapIDs = new()
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

    public static readonly HashSet<uint> PassageIDs =
    [
        2007188, // PotD
        2009507, // HoH
        2013287, // EO
        2014756  // PT
    ];

    public static readonly HashSet<uint> ReturnIDs =
    [
        2007187, // PotD
        2009506, // HoH
        2013286,  // EO
        2014755
    ];

    public static readonly HashSet<uint> HelpfulNpcNameIDs =
    [
        // HoH
        7396, 7397, 7398,
    ];

    public static unsafe bool IsChestOpenedOrFaded(this IGameObject obj)
    {
        var chest = (FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure*)obj.Address;
        return chest->Flags != FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure.TreasureFlags.None;
    }

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

    public static bool IsMimic(this IBattleNpc bNpc) => MimicNameIDs.Contains(bNpc.NameId);

    public static bool IsKerrigan(this IBattleNpc bNpc) => KorriganNameIDs.Contains(bNpc.NameId);

    public static bool IsHelpfulNpc(this IBattleNpc bNpc) => HelpfulNpcNameIDs.Contains(bNpc.NameId);

    public static bool IsFriendly(this IBattleNpc bNpc) => bNpc.IsHelpfulNpc() || bNpc.IsKerrigan();

    public static bool IsPassage(this IGameObject obj) => PassageIDs.Contains(obj.BaseId);

    public static bool IsReturn(this IGameObject obj) => ReturnIDs.Contains(obj.BaseId);

    public static bool IsVotive(this IGameObject obj) => Votive == obj.BaseId;

    public static bool IsBronzeChest(this IGameObject obj) => BronzeChestIDs.Contains(obj.BaseId);

    public static bool IsSilverChest(this IGameObject obj) => SilverChest == obj.BaseId;

    public static bool IsGoldChest(this IGameObject obj) => GoldChest == obj.BaseId;

    public static bool IsAccursedHoard(this IGameObject obj) => AccursedHoard == obj.BaseId || AccursedHoardCoffer == obj.BaseId;

    public static bool IsMimicChest(this IGameObject obj) => MimicChest == obj.BaseId;

    public static Vector2 Position2D(this IGameObject obj) => obj.Position.ToVector2();
}
