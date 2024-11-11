using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using DeepDungeonRadar.Maps;
using DeepDungeonRadar.Services;
using DeepDungeonRadar.Windows;

namespace DeepDungeonRadar;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "DeepDungeonRadar";
    public readonly WindowSystem WindowSystem = new("DeepDungeonRadar");
    private readonly RadarWindow radarWindow;
    private readonly ConfigWindow configWindow;
    private readonly DeepDungeonService deepDungeonService;
    private readonly MapDrawer mapDrawer;
    public Plugin(IDalamudPluginInterface pi)
    {
        Service.Initialize(this, pi);
        mapDrawer = new();
        deepDungeonService = new(mapDrawer);
        radarWindow = new(deepDungeonService, mapDrawer);
        configWindow = new();
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(radarWindow);

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ShowConfigWindow;
    }

    [Command("/ddr")]
    [HelpMessage("main command")]
    public void ConfigCommand(string command, string args)
    {
        if (args.IsNullOrWhitespace() || args == "config")
            ShowConfigWindow();
        else if (args == "toggle")
        {
            Service.Config.RadarEnabled ^= true;
            Service.Config.Save();
            Service.ChatGui.Print("[DeepDungeonRadar] Map " + (Service.Config.RadarEnabled ? "Enabled" : "Disabled") + ".");
        }
        else if (args == "cheat")
        {
            mapDrawer.Cheat();
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ShowConfigWindow;
        configWindow.Dispose();
        radarWindow.Dispose();
        deepDungeonService.Dispose();
        Service.Dispose();
    }
    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void ShowConfigWindow()
    {
        configWindow.IsOpen ^= true;
    }
}
