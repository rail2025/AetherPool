using System;
using System.Numerics;
using AetherPool.Game;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AetherPool.Windows
{
    public class TitleWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly GameSession gameSession;

        public TitleWindow(Plugin plugin, GameSession gameSession) : base("AetherPool###AetherPoolTitleWindow")
        {
            this.plugin = plugin;
            this.gameSession = gameSession;

            this.Size = new Vector2(400, 300);
            this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;
        }

        public void Dispose() { }

        public override void Draw()
        {
            var windowSize = ImGui.GetWindowSize();
            var buttonSize = new Vector2(windowSize.X * 0.6f, 30);
            var centerPosX = (windowSize.X - buttonSize.X) / 2;

            // Title
            var title = "AetherPool";
            var titleSize = ImGui.CalcTextSize(title);
            ImGui.SetCursorPosX((windowSize.X - titleSize.X) / 2);
            ImGui.SetCursorPosY(30f);
            ImGui.Text(title);

            // Game Mode Buttons
            ImGui.SetCursorPos(new Vector2(centerPosX, 80f));
            if (ImGui.Button("Play 8-Ball vs. AI", buttonSize))
            {
                gameSession.StartNewGame(GameMode.EightBall);
                this.IsOpen = false;
                plugin.MainWindow.IsOpen = true;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 115f));
            if (ImGui.Button("Play 9-Ball vs. AI", buttonSize))
            {
                gameSession.StartNewGame(GameMode.NineBall);
                this.IsOpen = false;
                plugin.MainWindow.IsOpen = true;
            }

            // Other Buttons
            ImGui.SetCursorPos(new Vector2(centerPosX, 160f));
            if (ImGui.Button("Settings", buttonSize))
            {
                plugin.ConfigWindow.IsOpen = true;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 195f));
            if (ImGui.Button("About", buttonSize))
            {
                plugin.AboutWindow.IsOpen = true;
            }
        }
    }
}
