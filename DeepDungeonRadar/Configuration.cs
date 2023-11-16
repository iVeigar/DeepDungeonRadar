using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using DeepDungeonRadar.Enums;

namespace DeepDungeonRadar;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public bool RadarEnabled { get; set; } = true;

    public uint RadarMapColor { get; set; } = 0xFF00E000; //Grass Green

    public bool RadarLockSizePos { get; set; } = false;

    public bool RadarClickThrough { get; set; } = false;

    public bool RadarShowInfo { get; set; } = true;

    public bool RadarOrientationFixed { get; set; } = true;

    public DetailLevel RadarDetailLevel { get; set; } = DetailLevel.仅物体名;

    public bool RadarTextStroke { get; set; } = true;

    public bool RadarShowCenter { get; set; } = true;

    public bool RadarShowAssistCircle { get; set; } = true;

    public float RadarObjectDotSize { get; set; } = 5f;

    public float RadarObjectDotStroke { get; set; } = 1f;

    public bool RadarUseLargeFont { get; set; } = false;

    public HashSet<DeepDungeonObject> DeepDungeonObjects { get; set; } = new();

    public float DeepDungeon_ObjectShowDistance { get; set; } = 100f;

    public bool DeepDungeon_EnableTrapView { get; set; } = true;

    public bool DeepDungeon_ShowObjectCount { get; set; } = true;

    public void Save()
    {
        Service.PluginInterface!.SavePluginConfig(this);
    }
}
