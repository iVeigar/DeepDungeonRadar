using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace DeepDungeonRadar.Extensions;

public static class GameObjectExtension
{
    private static readonly Dictionary<uint, string> NpcBaseMapping = new()
    {
        [2007358U] = "（金）",
        [2007357U] = "（银）",
        [2006020U] = "（拟态怪）",
        [2007542U] = "宝藏",
        [2007188U] = "转移石冢",
        [2007182U] = "地雷",
        [2007183U] = "诱饵陷阱",
        [2007184U] = "弱化陷阱",
        [2007185U] = "妨碍陷阱",
        [2007186U] = "蛙变陷阱",
        [2009504U] = "獭獭陷阱"
    };
    public static string GetDictionaryName(this GameObject obj)
    {
        if (NpcBaseMapping.TryGetValue(obj.DataId, out var value))
        {
            return value;
        }
        return obj.Name.TextValue;
    }

    public static Vector2 Location2D(this GameObject obj) => new(obj.Position.X, obj.Position.Z);

    public unsafe static uint ENpcIcon(this GameObject obj) => ((CSGameObject*)obj.Address)->NamePlateIconId;

    // Deep Dungeon
    public static bool IsTrap(this GameObject obj) => obj.DataId == 6388U && obj.Position != Vector3.Zero || obj.DataId >= 2007182U && obj.DataId <= 2007186U || obj.DataId == 2009504U || obj.DataId == 2013284U;

    public static bool IsAccursedHoard(this GameObject obj) => obj.DataId == 2007542U || obj.DataId == 2007543U;

    public static bool IsSilverCoffer(this GameObject obj) => obj.DataId == 2007357U;
}
