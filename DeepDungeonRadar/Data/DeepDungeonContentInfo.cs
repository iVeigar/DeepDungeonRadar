using System.Collections.Generic;

namespace DeepDungeonRadar.Data;

public static class DeepDungeonContentInfo
{
    public enum MimicChests
    {
        Bronze,
        Silver,
        Gold
    }

    public class FloorSetInfo
    {
        public int ContentId { get; internal init; }
        public int StartFloor { get; internal init; }
        public int RespawnTime { get; internal init; }
        public MimicChests MimicChests { get; internal init; }
    }

    public static readonly Dictionary<int, FloorSetInfo> ContentInfo = new()
    {
        // PotD
        { 60001, new FloorSetInfo { ContentId = 60001, StartFloor = 1, RespawnTime = 40, MimicChests = MimicChests.Bronze } },
        { 60002, new FloorSetInfo { ContentId = 60002, StartFloor = 11, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60003, new FloorSetInfo { ContentId = 60003, StartFloor = 21, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60004, new FloorSetInfo { ContentId = 60004, StartFloor = 31, RespawnTime = 60, MimicChests = MimicChests.Silver } },
        { 60005, new FloorSetInfo { ContentId = 60005, StartFloor = 41, RespawnTime = 120, MimicChests = MimicChests.Gold } },
        { 60006, new FloorSetInfo { ContentId = 60006, StartFloor = 51, RespawnTime = 60, MimicChests = MimicChests.Gold } },
        { 60007, new FloorSetInfo { ContentId = 60007, StartFloor = 61, RespawnTime = 60, MimicChests = MimicChests.Gold } },
        { 60008, new FloorSetInfo { ContentId = 60008, StartFloor = 71, RespawnTime = 60, MimicChests = MimicChests.Gold } },
        { 60009, new FloorSetInfo { ContentId = 60009, StartFloor = 81, RespawnTime = 60, MimicChests = MimicChests.Gold } },
        { 60010, new FloorSetInfo { ContentId = 60010, StartFloor = 91, RespawnTime = 120, MimicChests = MimicChests.Gold } },
        { 60011, new FloorSetInfo { ContentId = 60011, StartFloor = 101, RespawnTime = 90, MimicChests = MimicChests.Gold } },
        { 60012, new FloorSetInfo { ContentId = 60012, StartFloor = 111, RespawnTime = 90, MimicChests = MimicChests.Gold } },
        { 60013, new FloorSetInfo { ContentId = 60013, StartFloor = 121, RespawnTime = 90, MimicChests = MimicChests.Gold } },
        { 60014, new FloorSetInfo { ContentId = 60014, StartFloor = 131, RespawnTime = 90, MimicChests = MimicChests.Gold } },
        { 60015, new FloorSetInfo { ContentId = 60015, StartFloor = 141, RespawnTime = 90, MimicChests = MimicChests.Gold } },
        { 60016, new FloorSetInfo { ContentId = 60016, StartFloor = 151, RespawnTime = 300, MimicChests = MimicChests.Gold } },
        { 60017, new FloorSetInfo { ContentId = 60017, StartFloor = 161, RespawnTime = 300, MimicChests = MimicChests.Gold } },
        { 60018, new FloorSetInfo { ContentId = 60018, StartFloor = 171, RespawnTime = 300, MimicChests = MimicChests.Gold } },
        { 60019, new FloorSetInfo { ContentId = 60019, StartFloor = 181, RespawnTime = 300, MimicChests = MimicChests.Gold } },
        { 60020, new FloorSetInfo { ContentId = 60020, StartFloor = 191, RespawnTime = 300, MimicChests = MimicChests.Gold } },

        // Heaven on High
        { 60021, new FloorSetInfo { ContentId = 60021, StartFloor = 1, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60022, new FloorSetInfo { ContentId = 60022, StartFloor = 11, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60023, new FloorSetInfo { ContentId = 60023, StartFloor = 21, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60024, new FloorSetInfo { ContentId = 60024, StartFloor = 31, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60025, new FloorSetInfo { ContentId = 60025, StartFloor = 41, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60026, new FloorSetInfo { ContentId = 60026, StartFloor = 51, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60027, new FloorSetInfo { ContentId = 60027, StartFloor = 61, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60028, new FloorSetInfo { ContentId = 60028, StartFloor = 71, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60029, new FloorSetInfo { ContentId = 60029, StartFloor = 81, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60030, new FloorSetInfo { ContentId = 60030, StartFloor = 91, RespawnTime = 600, MimicChests = MimicChests.Gold } },

        // Eureka Orthos
        { 60031, new FloorSetInfo { ContentId = 60031, StartFloor = 1, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60032, new FloorSetInfo { ContentId = 60032, StartFloor = 11, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60033, new FloorSetInfo { ContentId = 60033, StartFloor = 21, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60034, new FloorSetInfo { ContentId = 60034, StartFloor = 31, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60035, new FloorSetInfo { ContentId = 60035, StartFloor = 41, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60036, new FloorSetInfo { ContentId = 60036, StartFloor = 51, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60037, new FloorSetInfo { ContentId = 60037, StartFloor = 61, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60038, new FloorSetInfo { ContentId = 60038, StartFloor = 71, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60039, new FloorSetInfo { ContentId = 60039, StartFloor = 81, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60040, new FloorSetInfo { ContentId = 60040, StartFloor = 91, RespawnTime = 600, MimicChests = MimicChests.Gold } },

        // Pilgrims Traverse
        { 60041, new FloorSetInfo { StartFloor = 1, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60042, new FloorSetInfo { StartFloor = 11, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60043, new FloorSetInfo { StartFloor = 21, RespawnTime = 60, MimicChests = MimicChests.Bronze } },
        { 60044, new FloorSetInfo { StartFloor = 31, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60045, new FloorSetInfo { StartFloor = 41, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60046, new FloorSetInfo { StartFloor = 51, RespawnTime = 600, MimicChests = MimicChests.Silver } },
        { 60047, new FloorSetInfo { StartFloor = 61, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60048, new FloorSetInfo { StartFloor = 71, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60049, new FloorSetInfo { StartFloor = 81, RespawnTime = 600, MimicChests = MimicChests.Gold } },
        { 60050, new FloorSetInfo { StartFloor = 91, RespawnTime = 600, MimicChests = MimicChests.Gold } },

    };
}
