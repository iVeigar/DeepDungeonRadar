using Dalamud.Configuration;
using DeepDungeonRadar.Utils;
using ECommons.Configuration;
namespace DeepDungeonRadar.Config;

public class Marker(uint Color, int Priority = 0)
{
    public uint Color = Color; 
    public int Priority = Priority;
}
public class IconScales
{
    public float EventObj = 1.0f;
    public float Chest = 0.75f;
    public float LocalPlayer = 1.4f;
    public float AccursedHoard = 0.45f;
    public float PassageArrow = 1.4f;
}
public class ObjectMarkerConfig
{
    public Marker Player = new(Color.Green, 10);
    public Marker Enemy = new(Color.White, 2);
    public Marker Friendly = new(Color.LightGreen, 4);
    public Marker GoldChest = new(Color.Gold, 6);
    public Marker SilverChest = new(Color.LightBlue, 6);
    public Marker BronzeChest = new(Color.Chocolate, 6);
    public Marker EventObj = new(Color.Cyan, 8);
}

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool RadarEnabled = true;
    public bool RadarOrientationFixed = true;
    public bool RadarClickThrough = false;
    public bool RadarLockSizePos = false;
    public bool RadarDrawUnreachable = true;

    public uint RadarSenseCircleOutlineColor = Color.Grey;

    public uint UnreachableAreaBorderColor = 0x60808080;
    public uint UnreachableAreaBackgroundColor = 0x40202020;
    public uint ReachableAreaBorderColor = 0xFA4291F4;
    public uint ReachableAreaBackgroundColor = 0x80247291;
    public uint RadarWindowBgColor = 0x60762622;

    public bool RadarObjectUseIcons = false;
    public IconScales IconScales = new();
    public ObjectMarkerConfig Markers = new();
    public float MarkerDotSize = 4;
    public bool RemoveNamePrefix = false;
    public string NamePrefixes = "地宫\n深宫\n天之\n正统\n交错路\n得到宽恕的\n召引";

    public bool ShowColliderBoxDot = true;
    public float RadarZoom = 1;
    
    public void Save() => EzConfig.Save();
}
