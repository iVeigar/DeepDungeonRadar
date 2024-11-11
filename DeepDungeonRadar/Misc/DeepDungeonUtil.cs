using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace DeepDungeonRadar.util;

[SuppressMessage("ReSharper", "PatternIsRedundant")] // RSRP-492231
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class DeepDungeonUtil
{
    public static Vector3 MeWorldPos => Service.ClientState.LocalPlayer?.Position ?? default;
    public static ushort MapId => Service.ClientState.TerritoryType;
    public static bool InDeepDungeon => InPotD || InHoH || InEO;
    public static bool InPotD => DataIds.PalaceOfTheDeadMapIds.Contains(MapId);
    public static bool InHoH => DataIds.HeavenOnHighMapIds.Contains(MapId);
    public static bool InEO => DataIds.EurekaOrthosMapIds.Contains(MapId);
    public static void PrintChatMessage(string msg)
    {
        var message = new XivChatEntry
        {
            Message = new SeStringBuilder()
                      .AddUiForeground($"[NecroLens] ", 48)
                      .Append(msg).Build()
        };

        Service.ChatGui.Print(message);
    }
}
