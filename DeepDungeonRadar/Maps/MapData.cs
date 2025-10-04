using System.Collections.Generic;
using System.Numerics;
using DeepDungeonRadar.Enums;


namespace DeepDungeonRadar.Maps;

public static class MapData
{
    public static readonly int[] LeafRooms = [1, 2, 3, 5, 9, 10, 14, 15, 19, 21, 22, 23, 26, 27, 28, 30, 34, 35, 39, 40, 44, 46, 47, 48];

    public static bool TryGetMapData(ushort territory, out (List<Vector2?>, (int, float)) map)
    {
        var bg = TerritoryToBg(territory);
        map = (GetMapRooms(bg), GetRoomShape(bg));
        return map.Item1 != null;
    }

    public static bool IsLeaf(int room, Direction direction)
    {
        return direction switch
        {
            Direction.正北 => room % 25 <= 4,
            Direction.正东 => room % 5 == 4,
            Direction.正南 => room % 25 >= 20,
            Direction.正西 => room % 5 == 0,
            _ => false
        };
    }

    public static int GetAdjacentRoom(int room, Direction direction)
    {
        var ret = -1;
        if (room >= 0 && room < 50)
        {
            var isLeaf = IsLeaf(room, direction);
            if (direction == Direction.正北 && !isLeaf)
                ret = room - 5;
            else if (direction == Direction.正东 && !isLeaf)
                ret = room + 1;
            else if (direction == Direction.正南 && !isLeaf)
                ret = room + 5;
            else if (direction == Direction.正西 && !isLeaf)
                ret = room - 1;
        }
        return ret;
    }

    private static DeepDungeonBg TerritoryToBg(ushort territory)
    {
        return territory switch
        {
            561 => DeepDungeonBg.f1c1,
            562 => DeepDungeonBg.f1c2,
            563 => DeepDungeonBg.f1c3,
            564 or 565 => DeepDungeonBg.f1c4,
            593 or 594 or 595 => DeepDungeonBg.f1c5,
            596 or 597 or 598 => DeepDungeonBg.f1c6,
            599 or 600 => DeepDungeonBg.f1c8,
            601 or 602 => DeepDungeonBg.f1c9,
            603 or 604 or 605 or 606 or 607 => DeepDungeonBg.f1c7,
            770 => DeepDungeonBg.e3c1,
            771 => DeepDungeonBg.e3c2,
            772 or 782 => DeepDungeonBg.e3c3,
            773 or 783 => DeepDungeonBg.e3c4,
            774 or 784 => DeepDungeonBg.e3c5,
            775 or 785 => DeepDungeonBg.e3c6,
            1099 => DeepDungeonBg.l5c1,
            1100 => DeepDungeonBg.l5c2,
            1101 or 1102 => DeepDungeonBg.l5c3,
            1103 or 1104 => DeepDungeonBg.l5c4,
            1105 or 1106 => DeepDungeonBg.l5c5,
            1107 or 1108 => DeepDungeonBg.l5c6,
            _ => DeepDungeonBg.notInKnownDeepDungeon,
        };
    }


    private static List<Vector2?> GetMapRooms(DeepDungeonBg bg)
    {
        return bg switch
        {
            DeepDungeonBg.f1c1 or DeepDungeonBg.f1c2 or DeepDungeonBg.f1c3 or DeepDungeonBg.f1c4 or DeepDungeonBg.f1c8 or DeepDungeonBg.f1c9 or DeepDungeonBg.l5c4 or DeepDungeonBg.l5c5 => PotD_1,// PotD 1-50, PotD 111-150, EO 41-80
            DeepDungeonBg.f1c5 or DeepDungeonBg.e3c3 => PotD_2,// PotD 51-80, HoH 21-30, HoH 61-70
            DeepDungeonBg.f1c6 => PotD_3,// PotD 81-110
            DeepDungeonBg.f1c7 => PotD_4,// PotD 151-200
            DeepDungeonBg.e3c1 or DeepDungeonBg.e3c2 => HoH_1,// HoH 1-20
            DeepDungeonBg.e3c4 or DeepDungeonBg.e3c5 => HoH_2,// HoH 31-50, 71-90
            DeepDungeonBg.e3c6 => HoH_3,// HoH 51-60, HoH 91-100
            DeepDungeonBg.l5c1 or DeepDungeonBg.l5c2 => EO_1,// EO 1-20
            DeepDungeonBg.l5c3 => EO_2,// EO 21-40
            DeepDungeonBg.l5c6 => EO_3,// EO 81-100
            _ => null
        };
    }

    private static (int, float) GetRoomShape(DeepDungeonBg bg)
    {
        // int: 0 = Rect, 1 = Circle
        // float: width or diameter
        return bg switch
        {
            DeepDungeonBg.f1c2 or DeepDungeonBg.f1c5 or DeepDungeonBg.f1c6 or DeepDungeonBg.f1c8 or DeepDungeonBg.f1c9 => (1, 40f), // PotD 11-20, PotD 51-130
            DeepDungeonBg.f1c7 => (1, 34f), // PotD 151-200
            DeepDungeonBg.e3c1 or DeepDungeonBg.e3c2 or DeepDungeonBg.e3c3 or DeepDungeonBg.e3c4 or DeepDungeonBg.e3c5 or DeepDungeonBg.e3c6 => (0, 35f), // HoH
            DeepDungeonBg.l5c1 or DeepDungeonBg.l5c2 => (1, 32f), // EO 1-20
            DeepDungeonBg.l5c3 => (0, 28f), // EO 21-40
            DeepDungeonBg.l5c6 => (1, 40f), // EO 81-100
            _ => (0, 40f)
        };
    }

    // 只是各房间中心的坐标，若干组楼层会复用同一套坐标，下划线前的部分仅代表该组坐标是使用了哪里的地形数据编写的
    private static readonly List<Vector2?> PotD_1 = new()
    {
        #region leftbottom
        null,
        new(-375f,188f),
        new(-288f,184f),
        new(-223f,200f),
        null,

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

        null,
        new(-367f,414f),
        new(-320f,415f),
        new(-223f,426f),
        null,
        #endregion
        #region righttop
        null,
        new(242f,-404f),
        new(292f,-416f),
        new(388f,-404f),
        null,

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

        null,
        new(242f,-184f),
        new(320f,-184f),
        new(378f,-196f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> PotD_2 = new()
    {
        #region leftbottom
        null,
        new(-372f,188f),
        new(-288f,184f),
        new(-224f,200f),
        null,

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

        null,
        new(-364f,412f),
        new(-280f,412f),
        new(-224f,428f),
        null,
        #endregion
        #region righttop
        null,
        new(244f,-408f),
        new(292f,-412f),
        new(380f,-408f),
        null,

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

        null,
        new(232f,-168f),
        new(320f,-188f),
        new(368f,-172f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> PotD_3 = new()
    {
        #region leftbottom
        null,
        new(-380f,182f),
        new(-288f,186f),
        new(-222f,194f),
        null,

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

        null,
        new(-370f,416f),
        new(-322f,416f),
        new(-222f,426f),
        null,
        #endregion
        #region righttop
        null,
        new(242f,-404f),
        new(290f,-418f),
        new(392f,-406f),
        null,

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

        null,
        new(240f,-184f),
        new(322f,-180f),
        new(380f,-194f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> PotD_4 = new()
    {
        #region leftbottom
        null,
        new(-376f,188f),
        new(-288f,192f),
        new(-220f,200f),
        null,

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

        null,
        new(-364f,409f),
        new(-300f,412f),
        new(-232f,420f),
        null,
        #endregion
        #region righttop
        null,
        new(244f,-400f),
        new(292f,-412f),
        new(388f,-400f),
        null,

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

        null,
        new(248f,-196f),
        new(320f,-192f),
        new(368f,-204f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> HoH_1 = new()
    {
        #region leftbottom
        null,
        new(-372f,188f),
        new(-288f,186f),
        new(-224f,200f),
        null,

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

        null,
        new(-364f,410f),
        new(-280f,412f),
        new(-224f,426f),
        null,
        #endregion
        #region righttop
        null,
        new(244f,-411f),
        new(292f,-411f),
        new(380f,-411f),
        null,

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

        null,
        new(234f,-171f),
        new(320f,-187f),
        new(366.5f,-173f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> HoH_2 = new()
    {
        #region leftbottom
        null,
        new(-356f,188f),
        new(-288f,180f),
        new(-232f,192f),
        null,

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

        null,
        new(-356f,412f),
        new(-300f,405f),
        new(-232f,426.5f),
        null,
        #endregion
        #region righttop
        null,
        new(244f,-400f),
        new(300f,-412f),
        new(368f,-400f),
        null,

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

        null,
        new(244f,-176f),
        new(300f,-196f),
        new(368f,-176f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> HoH_3 = new()
    {
        #region leftbottom
        null,
        new(-372f,188f),
        new(-288f,184f),
        new(-224f,200f),
        null,

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

        null,
        new(-364f,412f),
        new(-280f,412f),
        new(-224f,428f),
        null,
        #endregion
        #region righttop
        null,
        new(244f,-408f),
        new(292f,-412f),
        new(380f,-408f),
        null,

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

        null,
        new(232f,-172f),
        new(320f,-188f),
        new(368f,-172f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> EO_1 = new()
    {
        #region leftbottom
        null,
        new(-388f,156f),
        new(-300f,188f),
        new(-238f,174f),
        null,

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

        null,
        new(-364f,408f),
        new(-300f,420f),
        new(-238f,438f),
        null,
        #endregion
        #region righttop
        null,
        new(230f,-418f),
        new(300f,-424f),
        new(370f,-442f),
        null,

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

        null,
        new(220f,-180f),
        new(300f,-168f),
        new(346f,-174f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> EO_2 = new()
    {
        #region leftbottom
        null,
        new(-364f,172f),
        new(-300f,172f),
        new(-236f,172f),
        null,

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

        null,
        new(-364f,428f),
        new(-300f,428f),
        new(-236f,428f),
        null,
        #endregion
        #region righttop
        null,
        new(236f,-428f),
        new(300f,-428f),
        new(364f,-428f),
        null,

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

        null,
        new(236f,-172f),
        new(300f,-172f),
        new(364f,-172f),
        null,
        #endregion
    };
    private static readonly List<Vector2?> EO_3 = new()
    {
        #region leftbottom
        null,
        new(-380f,140f),
        new(-300f,140f),
        new(-220f,140f),
        null,

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

        null,
        new(-380f,460f),
        new(-300f,460f),
        new(-220f,460f),
        null,
        #endregion
        #region righttop
        null,
        new(220f,-460f),
        new(300f,-460f),
        new(380f,-460f),
        null,

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

        null,
        new(220f,-140f),
        new(300f,-140f),
        new(380f,-140f),
        null,
        #endregion
    };
}
