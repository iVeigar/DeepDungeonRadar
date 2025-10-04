using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;


namespace DeepDungeonRadar
{
    public class Service
    {
        public static Plugin Plugin { get; private set; }

        public static Configuration Config { get; private set; }
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }

        [PluginService] public static IBuddyList BuddyList { get; private set; }

        [PluginService] public static IChatGui ChatGui { get; private set; }

        [PluginService] public static IClientState ClientState { get; private set; }

        [PluginService] public static ICommandManager CommandManager { get; private set; }

        [PluginService] public static ICondition Condition { get; private set; }

        [PluginService] public static IDataManager DataManager { get; private set; }

        [PluginService] public static IFateTable FateTable { get; private set; }

        [PluginService] public static IFlyTextGui FlyTextGui { get; private set; }

        [PluginService] public static IFramework Framework { get; private set; }

        [PluginService] public static IGameGui GameGui { get; private set; }

        [PluginService] public static IJobGauges JobGauges { get; private set; }

        [PluginService] public static IKeyState KeyState { get; private set; }

        [PluginService] public static IObjectTable ObjectTable { get; private set; }

        [PluginService] public static IPartyFinderGui PartyFinderGui { get; private set; }

        [PluginService] public static IPartyList PartyList { get; private set; }

        [PluginService] public static ISigScanner SigScanner { get; private set; }

        [PluginService] public static ITargetManager TargetManager { get; private set; }

        [PluginService] public static IToastGui ToastGui { get; private set; }

        [PluginService] public static IDtrBar DtrBar { get; private set; }

        [PluginService] public static IPluginLog Log { get; private set; }

        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

        private static PluginCommandManager<IDalamudPlugin> PluginCommandManager;

        private Service(Plugin plugin, IDalamudPluginInterface pluginInterface)
        {
            Plugin = plugin;
            if (!pluginInterface.Inject(this))
            {
                Log.Error("Failed loading DalamudApi!");
                return;
            }
            Config ??= pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginCommandManager ??= new(plugin);
        }

        public static void Initialize(Plugin plugin, IDalamudPluginInterface pluginInterface) => _ = new Service(plugin, pluginInterface);

        public static void Dispose()
        {
            Config.Save();
            PluginCommandManager?.Dispose();
        }
    }

    #region PluginCommandManager
    public sealed class PluginCommandManager<T> : IDisposable where T : IDalamudPlugin
    {
        private readonly T plugin;
        private readonly (string, CommandInfo)[] pluginCommands;

        public PluginCommandManager(T p)
        {
            plugin = p;
            pluginCommands = plugin.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommandInfoTuple)
                .ToArray();

            AddCommandHandlers();
        }

        private void AddCommandHandlers()
        {
            foreach (var (command, commandInfo) in pluginCommands)
                Service.CommandManager.AddHandler(command, commandInfo);
        }

        private void RemoveCommandHandlers()
        {
            foreach (var (command, _) in pluginCommands)
                Service.CommandManager.RemoveHandler(command);
        }

        private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
        {
            var handlerDelegate = (IReadOnlyCommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(IReadOnlyCommandInfo.HandlerDelegate), plugin, method);

            var command = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
            var aliases = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
            var helpMessage = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
            var doNotShowInHelp = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();

            var commandInfo = new CommandInfo(handlerDelegate)
            {
                HelpMessage = helpMessage?.HelpMessage ?? string.Empty,
                ShowInHelp = doNotShowInHelp == null,
            };

            // Create list of tuples that will be filled with one tuple per alias, in addition to the base command tuple.
            var commandInfoTuples = new List<(string, CommandInfo)> { (command!.Command, commandInfo) };
            if (aliases != null)
                commandInfoTuples.AddRange(aliases.Aliases.Select(alias => (alias, commandInfo)));

            return commandInfoTuples;
        }

        public void Dispose()
        {
            RemoveCommandHandlers();
        }
    }
    #endregion

    #region Attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public AliasesAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }

        public CommandAttribute(string command)
        {
            Command = command;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DoNotShowInHelpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HelpMessageAttribute : Attribute
    {
        public string HelpMessage { get; }

        public HelpMessageAttribute(string helpMessage)
        {
            HelpMessage = helpMessage;
        }
    }
    #endregion
}
