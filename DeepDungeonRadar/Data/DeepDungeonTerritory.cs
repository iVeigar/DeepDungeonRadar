using System;
using System.Collections.Generic;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace DeepDungeonRadar.Data;

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

public static class DeepDungeonTerritory
{
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

    public static DeepDungeonBg ToBg(this uint territory) => Enum.TryParse<DeepDungeonBg>(Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territory).Bg.ToString().Split('/')[^1], true, out var bg) ? bg : DeepDungeonBg.none;

    public static bool InPotD(this uint territory) => PotDTerritoryIds.Contains(territory);

    public static bool InHoH(this uint territory) => HoHTerritoryIds.Contains(territory);

    public static bool InEO(this uint territory) => EOTerritoryIds.Contains(territory);

    public static bool InPT(this uint territory) => PTTerritoryIds.Contains(territory);
}
