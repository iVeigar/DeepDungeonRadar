using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using DeepDungeonRadar.Config;
using ECommons.DalamudServices;
using ECommons.MathHelpers;
using static DeepDungeonRadar.Radar.DeepDungeonData;
namespace DeepDungeonRadar.Radar;

public class RadarObject
{
    public enum RadarObjectKind
    {
        Player,
        HelpfulNpc,
        Kerrigan,
        Mimic,
        Enemy,
        Return,
        Passage,
        Votive,
        BronzeChest,
        SilverChest,
        GoldChest,
        MimicChest,
        AccursedHoard,
        Unknown,
    }

    private static readonly Dictionary<uint, string> CustomNames = new()
    {
        { SilverChestId, "（银）"},
        { GoldChestId, "（金）"},
        { MimicChestId, "（拟态怪）"},
        { AccursedHoardId, "（宝藏）" },
        { AccursedHoardCofferId, "（宝藏）" },
        //{ 2007182, "爆炸陷阱" },
        //{ 2007183, "诱饵陷阱" },
        //{ 2007184, "弱化陷阱" },
        //{ 2007185, "妨碍陷阱" },
        //{ 2007186, "蛙变陷阱" },
        //{ 2009504, "獭獭陷阱" },
        //{ 2013284, "猫头小鹰陷阱" },
        //{ 2014939, "妖灵陷阱" },
    };

    public RadarObject(IGameObject gameObject, DeepDungeonService deepDungeonService)
    {
        Obj = gameObject;
        DDS = deepDungeonService;
        if (Obj.IsPlayer())
            Kind = RadarObjectKind.Player;
        else if (Obj.IsGoldChest())
            Kind = RadarObjectKind.GoldChest;
        else if (Obj.IsSilverChest())
            Kind = RadarObjectKind.SilverChest;
        else if (Obj.IsBronzeChest())
            Kind = RadarObjectKind.BronzeChest;
        else if (Obj.IsMimicChest())
            Kind = RadarObjectKind.MimicChest;
        else if (Obj.IsAccursedHoard())
            Kind = RadarObjectKind.AccursedHoard;
        else if (Obj.IsVotive())
            Kind = RadarObjectKind.Votive;
        else if (Obj.IsPassage())
            Kind = RadarObjectKind.Passage;
        else if (Obj.IsReturn())
            Kind = RadarObjectKind.Return;
        else if (Obj.IsBattleNpc(out var b))
        {
            if (b.IsHelpfulNpc())
                Kind = RadarObjectKind.HelpfulNpc;
            else if (b.IsKerrigan())
                Kind = RadarObjectKind.Kerrigan;
            else if (b.IsMimic())
                Kind = RadarObjectKind.Mimic;
            else if (b.IsIgnoredBNpc())
                Kind = RadarObjectKind.Unknown;
            else
                Kind = RadarObjectKind.Enemy;
        }
    }
    private DeepDungeonService DDS { get; }

    public IGameObject Obj { get; }

    public RadarObjectKind Kind { get; set; } = RadarObjectKind.Unknown;

    public Vector2 Position => Obj.Position.ToVector2();

    public Marker GetMarkerConfig()
    {
        return Kind switch
        {
            RadarObjectKind.Player => Plugin.Config.Markers.Player,
            RadarObjectKind.Enemy or RadarObjectKind.Mimic => Plugin.Config.Markers.Enemy,
            RadarObjectKind.HelpfulNpc or RadarObjectKind.Kerrigan => Plugin.Config.Markers.Friendly,
            RadarObjectKind.MimicChest or RadarObjectKind.BronzeChest => Plugin.Config.Markers.BronzeChest,
            RadarObjectKind.Return or RadarObjectKind.Passage or RadarObjectKind.Votive => Plugin.Config.Markers.EventObj,
            RadarObjectKind.GoldChest or RadarObjectKind.AccursedHoard => Plugin.Config.Markers.GoldChest,
            RadarObjectKind.SilverChest => Plugin.Config.Markers.SilverChest,
            _ => null
        };
    }

    public string GetDisplayName()
    {
        if (CustomNames.TryGetValue(Obj.BaseId, out var value))
            return value;
        if (Kind == RadarObjectKind.BronzeChest)
            return "（铜）";
        var name = Obj.Name.TextValue;
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
            return $"D:{Obj.BaseId}" + (Obj is IBattleNpc b ? $" N:{b.NameId}" : "");
    }

    public bool HasIcon()
    {
        return Kind switch
        {
            RadarObjectKind.GoldChest or RadarObjectKind.SilverChest or RadarObjectKind.BronzeChest or RadarObjectKind.AccursedHoard or RadarObjectKind.Passage or RadarObjectKind.Return or RadarObjectKind.Votive => true,
            _ => false,
        };
    }

    public bool ShowName()
    {
        return !Plugin.Config.RadarObjectUseIcons || !HasIcon();
    }

    public bool ShouldDraw()
    {
        return Kind switch
        {
            RadarObjectKind.BronzeChest => !Obj.IsChestOpenedOrFaded(),
            RadarObjectKind.SilverChest or RadarObjectKind.GoldChest or RadarObjectKind.Votive => Obj.IsTargetable,
            RadarObjectKind.Return => Svc.Party.Length > 1,
            RadarObjectKind.AccursedHoard => !DDS.AccursedHoardOpened,
            RadarObjectKind.Enemy or RadarObjectKind.Mimic or RadarObjectKind.Kerrigan => !((IBattleChara)Obj).IsDead,
            RadarObjectKind.Unknown => false,
            _ => true,
        };
    }
}
