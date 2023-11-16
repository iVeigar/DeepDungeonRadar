namespace DeepDungeonRadar.Enums;

public enum DeepDungeonBg
{
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
    notInKnownDeepDungeon
}

public static class DeepDungeonBgExtension
{
    public static string GetName(this DeepDungeonBg bg) => bg switch
    {
        DeepDungeonBg.f1c1 => "死者宫殿 地下1～10层",
        DeepDungeonBg.f1c2 => "死者宫殿 地下11～20层",
        DeepDungeonBg.f1c3 => "死者宫殿 地下21～30层",
        DeepDungeonBg.f1c4 => "死者宫殿 地下31～50层",
        DeepDungeonBg.f1c5 => "死者宫殿 地下51～80层",
        DeepDungeonBg.f1c6 => "死者宫殿 地下81～110层",
        DeepDungeonBg.f1c8 => "死者宫殿 地下111～130层",
        DeepDungeonBg.f1c9 => "死者宫殿 地下131～150层",
        DeepDungeonBg.f1c7 => "死者宫殿 地下151～200层",
        DeepDungeonBg.e3c1 => "天之御柱 1～10层",
        DeepDungeonBg.e3c2 => "天之御柱 11～20层",
        DeepDungeonBg.e3c3 => "天之御柱 21～30层, 61～70层",
        DeepDungeonBg.e3c4 => "天之御柱 31～40层, 71～80层",
        DeepDungeonBg.e3c5 => "天之御柱 41～50层, 81～90层",
        DeepDungeonBg.e3c6 => "天之御柱 51～60层, 91～100层",
        DeepDungeonBg.l5c1 => "正统优雷卡 地下1～10层",
        DeepDungeonBg.l5c2 => "正统优雷卡 地下11～20层",
        DeepDungeonBg.l5c3 => "正统优雷卡 地下21～40层",
        DeepDungeonBg.l5c4 => "正统优雷卡 地下41～60层",
        DeepDungeonBg.l5c5 => "正统优雷卡 地下61～80层",
        DeepDungeonBg.l5c6 => "正统优雷卡 地下81～100层",
        _ => "未知区域"
    };
}