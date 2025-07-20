#pragma warning disable CA1416 // Suppress platform compatibility warnings

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using AetherPool.Windows;
using AetherPool.Audio;
using AetherPool.Game;
using Dalamud.Plugin.Services;

namespace AetherPool
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "AetherPool";
        private const string CommandName = "/aetherpool";

        [PluginService]
        internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService]
        internal static ITextureProvider TextureProvider { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("AetherPool");
        public AudioManager AudioManager { get; init; }
        public GameSession GameSession { get; init; }

        public MainWindow MainWindow { get; init; }
        public ConfigWindow ConfigWindow { get; init; }
        public TitleWindow TitleWindow { get; init; }
        public AboutWindow AboutWindow { get; init; }

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            AudioManager = new AudioManager();
            GameSession = new GameSession();

            MainWindow = new MainWindow(this, GameSession);
            ConfigWindow = new ConfigWindow(this);
            TitleWindow = new TitleWindow(this, GameSession);
            AboutWindow = new AboutWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(TitleWindow);
            WindowSystem.AddWindow(AboutWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the AetherPool game window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi += DrawMainWindow;

            TitleWindow.IsOpen = true;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            AudioManager.Dispose();
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi -= DrawMainWindow;
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            TitleWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void DrawMainWindow()
        {
            TitleWindow.IsOpen = true;
        }

        private void DrawConfigWindow()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
