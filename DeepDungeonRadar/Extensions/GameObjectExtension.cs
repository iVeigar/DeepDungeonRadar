using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using DeepDungeonRadar.Misc;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace DeepDungeonRadar.Extensions;

public static class GameObjectExtension
{
    private static readonly Dictionary<uint, string> NpcBaseMapping = new()
    {
        { 2007357, "（银）"},
        { 2007358, "（金）"},
        { 2006020,"（拟态怪）"},
        { 2007542, "埋藏点" },
        { 2007543, "宝藏" },
        { 2007182, "爆炸陷阱" },
        { 2007183, "诱饵陷阱" },
        { 2007184, "弱化陷阱" },
        { 2007185, "妨碍陷阱" },
        { 2007186, "蛙变陷阱" },
        { 2009504, "獭獭陷阱" },
        { 2013284, "猫头小鹰陷阱" }
    };
    public static string GetDictionaryName(this IGameObject obj)
    {
        if (NpcBaseMapping.TryGetValue(obj.DataId, out var value))
            return value;
        if (DataIds.BronzeChestIDs.Contains(obj.DataId))
            return "（铜）";
        return obj.Name.TextValue;
    }

    public static Vector2 Location2D(this IGameObject obj) => new(obj.Position.X, obj.Position.Z);

    public unsafe static uint ENpcIcon(this IGameObject obj) => ((CSGameObject*)obj.Address)->NamePlateIconId;

    // Deep Dungeon
    public static bool IsTrap(this IGameObject obj) => obj.DataId == 6388U && obj.Position != Vector3.Zero || obj.DataId >= 2007182U && obj.DataId <= 2007186U || obj.DataId == 2009504U || obj.DataId == 2013284U;

    public static bool IsAccursedHoard(this IGameObject obj) => obj.DataId == 2007542U || obj.DataId == 2007543U;

    public static bool IsSilverCoffer(this IGameObject obj) => obj.DataId == 2007357U;
}
