using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using DeepDungeonRadar.Windows;

namespace DeepDungeonRadar;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "DeepDungeonRadar";
    private readonly RadarUI radarUI;
    private readonly ConfigWindow configWindow;
    public readonly WindowSystem WindowSystem = new("DeepDungeonRadar");

    public Plugin(DalamudPluginInterface pi)
    {
        Service.Initialize(this, pi);

        radarUI = new RadarUI();
        configWindow = new ConfigWindow();
        WindowSystem.AddWindow(configWindow);

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ShowConfigWindow;
    }

    [Command("/ddr")]
    [HelpMessage("Open config window")]
    public void ConfigCommand(string command, string args)
    {
        ShowConfigWindow();
    }

    [Command("/ddrmap")]
    [HelpMessage("Toggle map")]
    public void ToggleMap(string command, string args)
    {
        Service.Config.RadarEnabled = !Service.Config.RadarEnabled;
        Service.Config.Save();
        Service.ChatGui.Print("[DeepDungeonRadar] Map " + (Service.Config.RadarEnabled ? "Enabled" : "Disabled") + ".");
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ShowConfigWindow;
        configWindow.Dispose();
        radarUI.Dispose();
        Service.Dispose();
    }
    private void DrawUI()
    {
        WindowSystem.Draw();
        radarUI.Draw();
    }

    public void ShowConfigWindow()
    {
        configWindow.IsOpen = true;
    }
}
