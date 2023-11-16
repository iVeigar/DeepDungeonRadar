using System.Collections.Generic;
using System.Numerics;
using Dalamud.Logging;
using DeepDungeonRadar.Enums;
using DeepDungeonRadar.UI;
using ImGuiNET;

namespace DeepDungeonRadar.Maps;

public static class DeepDungeonMap
{
    public static bool TryGetMap(DeepDungeonBg bg, out (List<Vector2>?, (int, float)) map)
    {
        map = (GetMapShape(bg), GetRoomShape(bg));
        return map.Item1 != null;
    }
    public static void DrawCurrentTerrytoryMap(this ImDrawListPtr windowDrawList, Vector3 pivotWorldPos, Vector2 pivotWindowPos, float zoom, float rotation, uint color)
    {
        if (!TryGetMap(Util.TerritoryToBg(Service.ClientState.TerritoryType), out var map))
        {
            PluginLog.Warning($"map not found, territory id: {Service.ClientState.TerritoryType}");
            return;
        }
        var (roomCenters, (roomShape, roomWidth)) = map;
        var halfWidth = roomWidth / 2;
        foreach (var roomCenter in roomCenters)
        {
            if (roomShape == 0)
            {
                Vector2[] corners = new Vector2[]
                {
                    roomCenter + new Vector2(-halfWidth, -halfWidth),
                    roomCenter + new Vector2(halfWidth, -halfWidth),
                    roomCenter + new Vector2(halfWidth, halfWidth),
                    roomCenter + new Vector2(-halfWidth, halfWidth)
                };
                for (var i = 0; i < corners.Length; i++)
                {
                    corners[i] = corners[i].ToRadarWindowPos(pivotWorldPos.ToVector2(), pivotWindowPos, zoom, rotation);
                }
                windowDrawList.AddConvexPolyFilled(ref corners[0], 4, color);
                windowDrawList.AddPolyline(ref corners[0], 4, 0x4F007800, ImDrawFlags.Closed, 1f);// TODO ÑÕÉ«
            }
            else if (roomShape == 1)
            {
                var circleRadius = halfWidth * zoom;
                var circleCenter = roomCenter.ToRadarWindowPos(pivotWorldPos.ToVector2(), pivotWindowPos, zoom, rotation);
                windowDrawList.AddCircleFilled(circleCenter, circleRadius, color);
                windowDrawList.AddCircle(circleCenter, circleRadius, 0x4F007800);
            }
        }
    }

    private static List<Vector2>? GetMapShape(DeepDungeonBg bg)
    {
        return bg switch
        {
            DeepDungeonBg.f1c1 or DeepDungeonBg.f1c2 or DeepDungeonBg.f1c3 or DeepDungeonBg.f1c4 or DeepDungeonBg.f1c8 or DeepDungeonBg.f1c9 or DeepDungeonBg.l5c4 or DeepDungeonBg.l5c5 => PotD_1,// PotD 1-5, PotD 12-15, EO 5-8
            DeepDungeonBg.f1c5 or DeepDungeonBg.e3c3 => PotD_2,// PotD 6-8, HoH 3, HoH 7
            DeepDungeonBg.f1c6 => PotD_3,// PotD 9-11
            DeepDungeonBg.f1c7 => PotD_4,// PotD 16-20
            DeepDungeonBg.e3c1 or DeepDungeonBg.e3c2 => HoH_1,// HoH 1-2
            DeepDungeonBg.e3c4 or DeepDungeonBg.e3c5 => HoH_2,// HoH 4-5, 8-9
            DeepDungeonBg.e3c6 => HoH_3,// HoH 6, HoH 10
            DeepDungeonBg.l5c1 or DeepDungeonBg.l5c2 => EO_1,// EO 1-2
            DeepDungeonBg.l5c3 => EO_2,// EO 3-4
            DeepDungeonBg.l5c6 => EO_3,// EO 9-10
            _ => null
        };
    }

    private static (int, float) GetRoomShape(DeepDungeonBg bg)
    {
        // int: 0 = Rect, 1 = Circle
        // float: width or diameter
        return bg switch
        {
            DeepDungeonBg.f1c7 => (1, 34f), // PotD 16-20
            DeepDungeonBg.e3c1 or DeepDungeonBg.e3c2 or DeepDungeonBg.e3c3 or DeepDungeonBg.e3c4 or DeepDungeonBg.e3c5 or DeepDungeonBg.e3c6 => (0, 35f), // HoH
            DeepDungeonBg.l5c1 or DeepDungeonBg.l5c2 => (1, 32f), // EO 1-2
            DeepDungeonBg.l5c3 => (0, 28f), // EO 3-4
            DeepDungeonBg.l5c6 => (1, 40f), // EO 9-10
            _ => (0, 40f)
        };
    }

    private static readonly List<Vector2> PotD_1 = new()
    {
        #region leftbottom
        new(-375f,188f),
        new(-288f,184f),
        new(-223f,200f),

        new(-401f,244f),
        new(-355f,244f),
        new(-300f,244f),
        new(-235f,256f),
        new(-179f,256f),

        new(-419f,300f),
        new(-367f,300f),
        new(-300f,300f),
        new(-235f,314f),
        new(-182f,314f),

        new(-409f,358f),
        new(-355f,358f),
        new(-300f,358f),
        new(-235f,370f),
        new(-181f,370f),

        new(-367f,414f),
        new(-320f,415f),
        new(-223f,426f),
        #endregion
        #region righttop
        new(242f,-404f),
        new(292f,-416f),
        new(388f,-404f),

        new(199f,-346f),
        new(254f,-346f),
        new(312f,-358f),
        new(368f,-346f),
        new(426f,-360f),

        new(184f,-300f),
        new(242f,-288f),
        new(300f,-300f),
        new(368f,-300f),
        new(430f,-312f),

        new(184f,-230f),
        new(242f,-242f),
        new(300f,-242f),
        new(366f,-254f),
        new(420f,-254f),

        new(242f,-184f),
        new(320f,-184f),
        new(378f,-196f)
        #endregion
    };
    private static readonly List<Vector2> PotD_2 = new()
    {
        #region leftbottom
        new(-372f,188f),
        new(-288f,184f),
        new(-224f,200f),

        new(-404f,244f),
        new(-352f,244f),
        new(-300f,244f),
        new(-236f,256f),
        new(-180f,256f),

        new(-416f,300f),
        new(-364f,300f),
        new(-300f,300f),
        new(-235f,312f),
        new(-184f,312f),

        new(-404f,356f),
        new(-352f,356f),
        new(-300f,356f),
        new(-236f,368f),
        new(-184f,368f),

        new(-364f,412f),
        new(-280f,412f),
        new(-224f,428f),
        #endregion
        #region righttop
        new(244f,-408f),
        new(292f,-412f),
        new(380f,-408f),

        new(204f,-344f),
        new(256f,-344f),
        new(312f,-356f),
        new(368f,-344f),
        new(424f,-356f),

        new(188f,-300f),
        new(244f,-288f),
        new(300f,-300f),
        new(356f,-288f),
        new(420f,-288f),

        new(192f,-232f),
        new(244f,-232f),
        new(300f,-244f),
        new(356f,-232f),
        new(408f,-232f),

        new(232f,-168f),
        new(320f,-188f),
        new(368f,-172f)
        #endregion
    };
    private static readonly List<Vector2> PotD_3 = new()
    {
        #region leftbottom
        new(-380f,182f),
        new(-288f,186f),
        new(-222f,194f),

        new(-414f,242f),
        new(-358f,242f),
        new(-300f,244f),
        new(-234f,252f),
        new(-178f,252f),

        new(-426f,300f),
        new(-370f,300f),
        new(-300f,300f),
        new(-234f,312f),
        new(-182f,312f),

        new(-414f,358f),
        new(-358f,358f),
        new(-300f,356f),
        new(-234f,368f),
        new(-180f,368f),

        new(-370f,416f),
        new(-322f,416f),
        new(-222f,426f),
        #endregion
        #region righttop
        new(242f,-404f),
        new(290f,-418f),
        new(392f,-406f),

        new(198f,-346f),
        new(254f,-346f),
        new(312f,-358f),
        new(370f,-346f),
        new(428f,-358f),

        new(184f,-300f),
        new(242f,-288f),
        new(300f,-300f),
        new(370f,-300f),
        new(428f,-312f),

        new(182f,-228f),
        new(240f,-240f),
        new(300f,-240f),
        new(368f,-252f),
        new(424f,-252f),

        new(240f,-184f),
        new(322f,-180f),
        new(380f,-194f)
        #endregion
    };
    private static readonly List<Vector2> PotD_4 = new()
    {
        #region leftbottom
        new(-376f,188f),
        new(-288f,192f),
        new(-220f,200f),

        new(-416f,244f),
        new(-364f,244f),
        new(-300f,248f),
        new(-232f,256f),
        new(-180f,256f),

        new(-404f,300f),
        new(-352f,300f),
        new(-300f,300f),
        new(-244f,312f),
        new(-192f,312f),

        new(-404f,352f),
        new(-352f,352f),
        new(-312f,356f),
        new(-244f,364f),
        new(-192f,364f),

        new(-364f,409f),
        new(-300f,412f),
        new(-232f,420f),
        #endregion
        #region righttop
        new(244f,-400f),
        new(292f,-412f),
        new(388f,-400f),

        new(204f,-344f),
        new(256f,-344f),
        new(312f,-356f),
        new(368f,-344f),
        new(424f,-356f),

        new(188f,-300f),
        new(244f,-288f),
        new(300f,-300f),
        new(352f,-300f),
        new(408f,-312f),

        new(192f,-236f),
        new(248f,-248f),
        new(300f,-248f),
        new(356f,-260f),
        new(408f,-260f),

        new(248f,-196f),
        new(320f,-192f),
        new(368f,-204f)
        #endregion
    };
    private static readonly List<Vector2> HoH_1 = new()
    {
        #region leftbottom
        new(-372f,188f),
        new(-288f,186f),
        new(-224f,200f),

        new(-404f,244f),
        new(-352f,244f),
        new(-300f,244f),
        new(-236f,255f),
        new(-192f,255f),

        new(-418f,300f),
        new(-364f,300f),
        new(-300f,300f),
        new(-236f,312f),
        new(-180f,312f),

        new(-404f,356f),
        new(-352f,356f),
        new(-300f,356f),
        new(-236f,368f),
        new(-184f,368f),

        new(-364f,410f),
        new(-280f,412f),
        new(-224f,426f),
        #endregion
        #region righttop
        new(244f,-411f),
        new(292f,-411f),
        new(380f,-411f),

        new(202f,-345f),
        new(256f,-345f),
        new(312f,-357f),
        new(368f,-345f),
        new(432f,-357f),

        new(188f,-301f),
        new(244f,-289f),
        new(300f,-301f),
        new(356f,-289f),
        new(410f,-289f),

        new(195.5f,-231f),
        new(246f,-231f),
        new(300f,-243f),
        new(354.5f,-231f),
        new(405f,-231f),

        new(234f,-171f),
        new(320f,-187f),
        new(366.5f,-173f)
        #endregion
    };
    private static readonly List<Vector2> HoH_2 = new()
    {
        #region leftbottom
        new(-356f,188f),
        new(-288f,180f),
        new(-232f,192f),

        new(-392f,244f),
        new(-344f,244f),
        new(-300f,240f),
        new(-244f,252f),
        new(-193.5f,252f),

        new(-412f,300f),
        new(-356f,300f),
        new(-300f,300f),
        new(-244f,312f),
        new(-188f,312f),

        new(-392f,356f),
        new(-344f,356f),
        new(-300f,356f),
        new(-244f,368f),
        new(-188f,368f),

        new(-356f,412f),
        new(-300f,405f),
        new(-232f,426.5f),
        #endregion
        #region righttop
        new(244f,-400f),
        new(300f,-412f),
        new(368f,-400f),

        new(200f,-344f),
        new(256f,-344f),
        new(312f,-356f),
        new(368f,-344f),
        new(424f,-356f),

        new(180f,-300f),
        new(240f,-288f),
        new(300f,-300f),
        new(372f,-300f),
        new(420f,-300f),

        new(196f,-232f),
        new(244f,-232f),
        new(300f,-244f),
        new(356f,-232f),
        new(404f,-232f),

        new(244f,-176f),
        new(300f,-196f),
        new(368f,-176f)
        #endregion
    };
    private static readonly List<Vector2> HoH_3 = new()
    {
        #region leftbottom
        new(-372f,188f),
        new(-288f,184f),
        new(-224f,200f),

        new(-404f,244f),
        new(-352f,244f),
        new(-300f,244f),
        new(-236f,256f),
        new(-180f,256f),

        new(-416f,300f),
        new(-364f,300f),
        new(-300f,300f),
        new(-235f,312f),
        new(-184f,312f),

        new(-404f,356f),
        new(-352f,356f),
        new(-300f,356f),
        new(-236f,368f),
        new(-184f,368f),

        new(-364f,412f),
        new(-280f,412f),
        new(-224f,428f),
        #endregion
        #region righttop
        new(244f,-408f),
        new(292f,-412f),
        new(380f,-408f),

        new(204f,-344f),
        new(256f,-344f),
        new(312f,-356f),
        new(368f,-344f),
        new(424f,-356f),

        new(188f,-300f),
        new(244f,-288f),
        new(300f,-300f),
        new(356f,-288f),
        new(420f,-288f),

        new(192f,-232f),
        new(244f,-232f),
        new(300f,-244f),
        new(356f,-232f),
        new(408f,-232f),

        new(232f,-172f),
        new(320f,-188f),
        new(368f,-172f)
        #endregion
    };
    private static readonly List<Vector2> EO_1 = new()
    {
        #region leftbottom
        new(-388f,156f),
        new(-300f,188f),
        new(-238f,174f),

        new(-420f,220f),
        new(-364f,220f),
        new(-300f,244f),
        new(-238f,230f),
        new(-182f,230f),

        new(-432f,300f),
        new(-364f,276f),
        new(-300f,300f),
        new(-238f,300f),
        new(-176f,300f),

        new(-426f,346f),
        new(-364f,346f),
        new(-300f,364f),
        new(-214f,370f),
        new(-158f,370f),

        new(-364f,408f),
        new(-300f,420f),
        new(-238f,438f),
        #endregion
        #region righttop
        new(230f,-418f),
        new(300f,-424f),
        new(370f,-442f),

        new(174f,-362f),
        new(230f,-362f),
        new(300f,-362f),
        new(370f,-386f),
        new(438f,-362f),

        new(188f,-300f),
        new(244f,-300f),
        new(300f,-300f),
        new(364f,-300f),
        new(420f,-300f),

        new(152f,-212f),
        new(220f,-236f),
        new(276f,-236f),
        new(346f,-236f),
        new(408f,-236f),

        new(220f,-180f),
        new(300f,-168f),
        new(346f,-174f)
        #endregion
    };
    private static readonly List<Vector2> EO_2 = new()
    {
        #region leftbottom
        new(-364f,172f),
        new(-300f,172f),
        new(-236f,172f),

        new(-428f,236f),
        new(-364f,236f),
        new(-300f,236f),
        new(-236f,236f),
        new(-172f,236f),

        new(-428f,300f),
        new(-364f,300f),
        new(-300f,300f),
        new(-236f,300f),
        new(-172f,300f),

        new(-428f,364f),
        new(-364f,364f),
        new(-300f,364f),
        new(-236f,364f),
        new(-172f,364f),

        new(-364f,428f),
        new(-300f,428f),
        new(-236f,428f),
        #endregion
        #region righttop
        new(236f,-428f),
        new(300f,-428f),
        new(364f,-428f),

        new(172f,-364f),
        new(236f,-364f),
        new(300f,-364f),
        new(364f,-364f),
        new(428f,-364f),

        new(172f,-300f),
        new(236f,-300f),
        new(300f,-300f),
        new(364f,-300f),
        new(428f,-300f),

        new(172f,-236f),
        new(236f,-236f),
        new(300f,-236f),
        new(364f,-236f),
        new(428f,-236f),

        new(236f,-172f),
        new(300f,-172f),
        new(364f,-172f),
        #endregion
    };
    private static readonly List<Vector2> EO_3 = new()
    {
        #region leftbottom
        new(-380f,140f),
        new(-300f,140f),
        new(-220f,140f),

        new(-460f,220f),
        new(-380f,220f),
        new(-300f,220f),
        new(-220f,220f),
        new(-140f,220f),

        new(-460f,300f),
        new(-380f,300f),
        new(-300f,300f),
        new(-220f,300f),
        new(-140f,300f),

        new(-460f,380f),
        new(-380f,380f),
        new(-300f,380f),
        new(-220f,380f),
        new(-140f,380f),

        new(-380f,460f),
        new(-300f,460f),
        new(-220f,460f),
        #endregion
        #region righttop
        new(220f,-460f),
        new(300f,-460f),
        new(380f,-460f),

        new(140f,-380f),
        new(220f,-380f),
        new(300f,-380f),
        new(380f,-380f),
        new(460f,-380f),

        new(140f,-300f),
        new(220f,-300f),
        new(300f,-300f),
        new(380f,-300f),
        new(460f,-300f),

        new(140f,-220f),
        new(220f,-220f),
        new(300f,-220f),
        new(380f,-220f),
        new(460f,-220f),

        new(220f,-140f),
        new(300f,-140f),
        new(380f,-140f),
        #endregion
    };
}
