using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using DeepDungeonRadar.Services;
using DeepDungeonRadar.Windows;

namespace DeepDungeonRadar;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "DeepDungeonRadar";
    private readonly RadarWindow radarWindow;
    private readonly ConfigWindow configWindow;
    private readonly TnTService tntService;
    public readonly WindowSystem WindowSystem = new("DeepDungeonRadar");

    public Plugin(DalamudPluginInterface pi)
    {
        PluginService.Initialize(this, pi);

        radarWindow = new();
        configWindow = new();
        tntService = new();
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(radarWindow);

        PluginService.PluginInterface.UiBuilder.Draw += DrawUI;
        PluginService.PluginInterface.UiBuilder.OpenConfigUi += ShowConfigWindow;
    }

    [Command("/ddr")]
    [HelpMessage("main command")]
    public void ConfigCommand(string command, string args)
    {
        if (args.IsNullOrWhitespace() || args == "config")
            ShowConfigWindow();
        else if(args == "toggle")
        {
            PluginService.Config.RadarEnabled = !PluginService.Config.RadarEnabled;
            PluginService.Config.Save();
            PluginService.ChatGui.Print("[DeepDungeonRadar] Map " + (PluginService.Config.RadarEnabled ? "Enabled" : "Disabled") + ".");
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        PluginService.PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginService.PluginInterface.UiBuilder.OpenConfigUi -= ShowConfigWindow;
        configWindow.Dispose();
        radarWindow.Dispose();
        tntService.Dispose();
        PluginService.Dispose();
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
