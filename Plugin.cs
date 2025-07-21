#pragma warning disable CA1416

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using AetherPool.Windows;
using AetherPool.Audio;
using AetherPool.Game;
using AetherPool.Networking;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Conditions;

namespace AetherPool
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "AetherPool";
        private const string CommandName = "/aetherpool";

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal IPartyList PartyList { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;

        private bool wasDead = false;
        
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("AetherPool");
        public AudioManager AudioManager { get; init; }
        public GameSession GameSession { get; init; }
        public NetworkManager NetworkManager { get; init; }
        public MultiplayerGameSession? MultiplayerGameSession { get; private set; }

        public MainWindow MainWindow { get; init; }
        public ConfigWindow ConfigWindow { get; init; }
        public TitleWindow TitleWindow { get; init; }
        public AboutWindow AboutWindow { get; init; }
        public MultiplayerWindow MultiplayerWindow { get; init; }


        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            AudioManager = new AudioManager();
            GameSession = new GameSession();
            NetworkManager = new NetworkManager();

            MainWindow = new MainWindow(this, GameSession);
            ConfigWindow = new ConfigWindow(this);
            TitleWindow = new TitleWindow(this, GameSession);
            AboutWindow = new AboutWindow(this);
            MultiplayerWindow = new MultiplayerWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(TitleWindow);
            WindowSystem.AddWindow(AboutWindow);
            WindowSystem.AddWindow(MultiplayerWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the AetherPool game window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi += DrawMainWindow;

            NetworkManager.OnConnected += OnNetworkConnected;
            NetworkManager.OnDisconnected += OnNetworkDisconnected;
            NetworkManager.OnError += OnNetworkError;
            NetworkManager.OnStateUpdateReceived += OnStateUpdateReceived;
            NetworkManager.OnRoomClosingWarning += OnRoomClosingWarning;

            ClientState.TerritoryChanged += OnTerritoryChanged;
            Condition.ConditionChange += OnConditionChanged;
          
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            AudioManager.Dispose();
            NetworkManager.Dispose();

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi -= DrawMainWindow;

            NetworkManager.OnConnected -= OnNetworkConnected;
            NetworkManager.OnDisconnected -= OnNetworkDisconnected;
            NetworkManager.OnError -= OnNetworkError;
            NetworkManager.OnStateUpdateReceived -= OnStateUpdateReceived;
            NetworkManager.OnRoomClosingWarning -= OnRoomClosingWarning;

            ClientState.TerritoryChanged -= OnTerritoryChanged;
            Condition.ConditionChange -= OnConditionChanged;
            
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            TitleWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        private void DrawMainWindow()
        {
            TitleWindow.IsOpen = true;
        }

        private void DrawConfigWindow()
        {
            ConfigWindow.IsOpen = true;
        }

        #region Networking Event Handlers
        private void OnNetworkConnected()
        {
            MultiplayerWindow.SetConnectionStatus("Connected!", false);
            MultiplayerGameSession = new MultiplayerGameSession(NetworkManager);
            MultiplayerWindow.IsOpen = false;
            TitleWindow.IsOpen = false;
            MainWindow.StartMultiplayerGame();
            MainWindow.IsOpen = true;
        }

        private void OnNetworkDisconnected()
        {
            MultiplayerWindow.SetConnectionStatus("Disconnected.", false);
            if (MultiplayerGameSession != null)
            {
                MultiplayerGameSession.GoToMainMenu();
                MultiplayerGameSession = null;
            }
            if (MainWindow.IsOpen && MainWindow.IsMultiplayer)
            {
                MainWindow.IsOpen = false;
                TitleWindow.IsOpen = true;
            }
        }

        private void OnNetworkError(string error)
        {
            MultiplayerWindow.SetConnectionStatus(error, true);
        }

        private void OnStateUpdateReceived(NetworkPayload payload)
        {
            MultiplayerGameSession?.HandleNetworkPayload(payload);
        }

        private void OnRoomClosingWarning()
        {
            OnNetworkDisconnected();
        }
        #endregion

        private void OnTerritoryChanged(ushort territoryTypeId)
        {
            // Close the window on territory change to prevent issues
            if (MainWindow.IsOpen) { MainWindow.IsOpen = false; }
            if (TitleWindow.IsOpen) { TitleWindow.IsOpen = false; }
        }

        private void OnConditionChanged(ConditionFlag flag, bool value)
        {
            // Open on Death
            if (flag == ConditionFlag.InCombat && !value)
            {
                bool isDead = ClientState.LocalPlayer?.CurrentHp == 0;
                if (isDead && !wasDead && Configuration.OpenOnDeath)
                {
                    TitleWindow.IsOpen = true;
                }
                wasDead = isDead;
            }

            // Open in Duty Queue
            if (flag == ConditionFlag.WaitingForDuty && value && Configuration.OpenInQueue)
            {
                TitleWindow.IsOpen = true;
            }
            // Open in Party Finder
            if (flag == ConditionFlag.UsingPartyFinder && value && Configuration.OpenInPartyFinder)
            {
                TitleWindow.IsOpen = true;
            }
            // Open during Crafting
            if (flag == ConditionFlag.Crafting && value && Configuration.OpenDuringCrafting)
            {
                TitleWindow.IsOpen = true;
            }
        }
        
    }
}
