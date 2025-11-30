using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using DeepDungeonRadar.Config;
using DeepDungeonRadar.Radar;
using ECommons;
using ECommons.Commands;
using ECommons.Configuration;
using ECommons.DalamudServices;
namespace DeepDungeonRadar;

public sealed class Plugin : IDalamudPlugin
{
    public readonly WindowSystem WindowSystem = new("DeepDungeonRadar");
    private readonly RadarWindow radarWindow;
    private readonly ConfigWindow configWindow;
    private readonly DeepDungeonService deepDungeonService;
    private readonly ColliderBoxService colliderBoxService;
    private readonly MapService mapService;
    public static Configuration Config { get; private set; }

    public Plugin(IDalamudPluginInterface pi)
    {
        ECommonsMain.Init(pi, this);
        EzConfig.Migrate<Configuration>();
        Config = EzConfig.Init<Configuration>();

        deepDungeonService = new();
        colliderBoxService = new(deepDungeonService);
        mapService = new(deepDungeonService, colliderBoxService);
        Svc.Framework.RunOnFrameworkThread(() => ToggleRadar(Config.RadarEnabled));
        radarWindow = new(deepDungeonService, mapService);
        configWindow = new(this);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(radarWindow);
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
    }

    [Cmd("/ddr", """
        打开设置窗口
        /ddr config → 打开设置窗口
        /ddr toggle → 启用/禁用小地图
        /ddr cheat → ???
        """)]
    public void ConfigCommand(string _, string args)
    {
        if (args.IsNullOrWhitespace() || args == "config")
            ToggleConfigWindow();
        else if (args == "toggle")
        {
            Config.RadarEnabled ^= true;
            Config.Save();
            ToggleRadar(Config.RadarEnabled);
        }
        else if (args == "cheat")
        {
            colliderBoxService.Cheat();
        }
    }

    public void ToggleRadar(bool enabled)
    {
        if (enabled)
        {
            mapService.RegisterEvents();
            if (deepDungeonService.IsRadarReady)
                mapService.OnEnteredNewFloor();
        }
        else
        {
            mapService.UnregisterEvents();
            mapService.OnExitingCurrentFloor(true);
        }
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;
        WindowSystem.RemoveAllWindows();
        mapService.Dispose();
        deepDungeonService.Dispose();
        EzConfig.Save();
        ECommonsMain.Dispose();
    }
    private void Draw()
    {
        WindowSystem.Draw();
        if (Config.ShowColliderBoxDot)
            colliderBoxService.Draw();
    }

    public void ToggleConfigWindow()
    {
        configWindow.IsOpen ^= true;
    }

    public static void PrintChatMessage(string msg)
    {
        var message = new XivChatEntry
        {
            Message = new SeStringBuilder()
                      .AddUiForeground($"[深宫小地图] ", 48)
                      .Append(msg).Build()
        };

        Svc.Chat.Print(message);
    }
}
