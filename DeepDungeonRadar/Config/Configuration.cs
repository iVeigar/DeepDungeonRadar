using Dalamud.Configuration;
using DeepDungeonRadar.Utils;
using ECommons.Configuration;
namespace DeepDungeonRadar.Config;

public struct Marker(uint Color, uint StrokeColor = Color.Black, bool ShowName = true, int Priority = 0)
{
    public uint Color = Color; 
    public uint StrokeColor = StrokeColor;
    public bool ShowName = ShowName;
    public int Priority = Priority;
}

public class ObjectMarkerConfig
{
    public Marker Player = new(Color.Green, Priority: 10);
    public Marker Enemy = new(Color.White, Priority: 2);
    public Marker Friendly = new(Color.LightGreen, Priority: 4);
    public Marker GoldChest = new(Color.Gold, Priority: 6);
    public Marker SilverChest = new(Color.LightBlue, Priority: 6);
    public Marker BronzeChest = new(Color.Chocolate, Priority: 6);
    public Marker AccursedHoard = new(Color.Gold, Priority: 6);
    public Marker EventObj = new(Color.Cyan, Priority: 8);
}

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool RadarEnabled = true;
    public bool RadarOrientationFixed = true;
    public bool RadarClickThrough = false;
    public bool RadarLockSizePos = false;

    public bool RadarDrawUnreachable = true;
    public uint UnreachableAreaBorderColor = 0x60808080;
    public uint UnreachableAreaBackgroundColor = 0x40202020;
    public uint ReachableAreaBorderColor = 0xE0FDE888;
    public uint ReachableAreaBackgroundColor = 0xE0505050;
    public uint RadarSenseCircleOutlineColor = Color.Grey;
    public float RadarPassageArrowScale = 1.0f;
    public bool ShowColliderBoxDot = true;
    public bool RemoveNamePrefix = false;
    public string NamePrefixes = "地宫\n深宫\n天之\n正统\n交错路\n得到宽恕的\n召引";

    public ObjectMarkerConfig Markers = new();
    public float MarkerDotSize = 4;
    public float RadarZoom = 1;
    
    public void Save() => EzConfig.Save();
}
